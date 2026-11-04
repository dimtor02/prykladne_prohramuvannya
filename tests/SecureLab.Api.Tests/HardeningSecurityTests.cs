using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecureLab.Api.Data;
using SecureLab.Api.Security;

namespace SecureLab.Api.Tests;

public sealed class HardeningSecurityTests : IAsyncLifetime
{
    private readonly SecureLabApiFactory factory = new();
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => factory.DisposeAsync().AsTask();

    private static object Body(string title) => new
    {
        title, description = "Controlled LR06 regression record", severity = "Low", occurredAtUtc = DateTimeOffset.UtcNow
    };

    private async Task<string> Snapshot()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        return JsonSerializer.Serialize(new
        {
            incidents = await db.Incidents.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Title, x.Description, x.Status, x.OwnerUserId, x.UpdatedAtUtc }).ToListAsync(),
            history = await db.IncidentStatusHistory.AsNoTracking().OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.IncidentId, x.NewStatus, x.ChangedByUserId }).ToListAsync()
        });
    }

    [Theory]
    [InlineData("create", false)] [InlineData("create", true)]
    [InlineData("update", false)] [InlineData("update", true)]
    [InlineData("delete", false)] [InlineData("delete", true)]
    [InlineData("triage", false)] [InlineData("triage", true)]
    [InlineData("logout", false)] [InlineData("logout", true)]
    public async Task REG_CSRF_Perimeter_RejectsMissingOrInvalidToken_WithoutStateChange(string action, bool invalid)
    {
        using var client = await factory.LoginSeedAsync(action == "triage" ? "morgan" : "alice");
        if (invalid)
        {
            // Valid cookie half, deliberately invalid request half.
            await client.UseAntiforgeryTokenAsync();
            client.DefaultRequestHeaders.Remove(AntiforgeryFilter.HeaderName);
            client.DefaultRequestHeaders.Add(AntiforgeryFilter.HeaderName, "invalid-study-token");
        }
        var before = await Snapshot();
        var path = $"/api/incidents/{DbSeeder.SafeTextIncidentId}";
        using var response = action switch
        {
            "create" => await client.PostAsJsonAsync("/api/incidents", Body("LR06 blocked " + Guid.NewGuid())),
            "update" => await client.PutAsJsonAsync(path, new { title = "Blocked update", description = "Blocked description" }),
            "delete" => await client.DeleteAsync(path),
            "triage" => await client.PostAsync(path + "/triage", null),
            _ => await client.PostAsync("/api/auth/logout", null)
        };
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("urn:securelab:csrf", problem.GetProperty("type").GetString());
        Assert.False(problem.TryGetProperty("errors", out _));
        Assert.Equal(before, await Snapshot());
        using var session = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
    }

    [Fact]
    public async Task REG_CSRF_01_RealSession_DenialStateRead_Token_AllowedUpdate_StateRead()
    {
        using var client = await factory.LoginSeedAsync("alice");
        // Create an independent fixture, then explicitly remove the request token.
        await client.UseAntiforgeryTokenAsync();
        using var create = await client.PostAsJsonAsync("/api/incidents", Body("LR06 flow " + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var data = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = data.GetProperty("id").GetGuid();
        var path = $"/api/incidents/{id}";
        try
        {
            client.DefaultRequestHeaders.Remove(AntiforgeryFilter.HeaderName);
            var before = await client.GetStringAsync(path);
            var edit = new { title = data.GetProperty("title").GetString(), description = "Permitted token-protected update" };
            using var denied = await client.PutAsJsonAsync(path, edit);
            Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
            Assert.Equal(before, await client.GetStringAsync(path));
            await client.UseAntiforgeryTokenAsync();
            using var allowed = await client.PutAsJsonAsync(path, edit);
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            var after = await client.GetFromJsonAsync<JsonElement>(path);
            Assert.Equal(edit.description, after.GetProperty("description").GetString());
            using var delete = await client.DeleteAsync(path);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            using var gone = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<SecureLabDbContext>().Incidents.Where(x => x.Id == id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Token_IsBoundToPrincipal_OldTokenFailsAfterLogoutAndNewLogin()
    {
        using var client = await factory.LoginSeedAsync("alice");
        await client.UseAntiforgeryTokenAsync();
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var anonymousToken = await client.GetAsync(AntiforgeryFilter.TokenRoute);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousToken.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "bob", password = Environment.GetEnvironmentVariable("SeedUsers__Password")
        });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var before = await Snapshot();
        using var oldToken = await client.PostAsJsonAsync("/api/incidents", Body("Stale token must fail"));
        Assert.Equal(HttpStatusCode.BadRequest, oldToken.StatusCode);
        Assert.Equal(before, await Snapshot());
        await client.UseAntiforgeryTokenAsync();
        using var newLogout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, newLogout.StatusCode);
    }

    [Fact]
    public async Task RequestToken_WithoutItsCookie_IsRejected()
    {
        using var first = await factory.LoginSeedAsync("alice");
        using var second = await factory.LoginSeedAsync("alice");
        await first.UseAntiforgeryTokenAsync();
        second.DefaultRequestHeaders.Add(AntiforgeryFilter.HeaderName, first.DefaultRequestHeaders.GetValues(AntiforgeryFilter.HeaderName));
        using var denied = await second.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using var stillLoggedIn = await second.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, stillLoggedIn.StatusCode);
    }

    [Fact]
    public async Task TokenResponse_IsNotCached_AndTeachingOriginCannotReadIt()
    {
        using var client = await factory.LoginSeedAsync("alice");
        using var request = new HttpRequestMessage(HttpMethod.Get, AntiforgeryFilter.TokenRoute);
        request.Headers.Add("Origin", "http://localhost:4173");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.CacheControl?.NoCache);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("SecureLab.Antiforgery=", StringComparison.Ordinal));
        // Only attributes are inspected, never print the cookie value on assertion failure.
        var attributes = cookie[(cookie.IndexOf(';') + 1)..].ToLowerInvariant();
        Assert.Contains("httponly", attributes);
        Assert.Contains("samesite=strict", attributes);
        Assert.Contains("secure", attributes);
    }

    [Fact]
    public async Task REG_RATE_SEARCH_TwoPrincipals_IsolatedQuota_Recovery_AuthzRemainsSeparate()
    {
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.PostConfigure<SearchRateLimitOptions>(o => { o.PermitLimit = 3; o.WindowSeconds = 3; })));
        using var alice = await host.LoginSeedAsync("alice");
        using var aliceAgain = await host.LoginSeedAsync("alice");
        using var bob = await host.LoginSeedAsync("bob");
        for (var i = 0; i < 3; i++)
        {
            using var ok = await alice.GetAsync("/api/incidents/search?q=VPN&sortBy=createdAtUtc");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }
        aliceAgain.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2.100");
        aliceAgain.DefaultRequestHeaders.Add("X-Demo-UserId", DbSeeder.BobId.ToString());
        using var denied = await aliceAgain.GetAsync("/api/incidents/search?q=changed&client=bob");
        Assert.Equal(HttpStatusCode.TooManyRequests, denied.StatusCode);
        Assert.NotNull(denied.Headers.RetryAfter);
        Assert.Equal("urn:securelab:search-rate-limit", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString());
        using var independent = await bob.GetAsync("/api/incidents/search?q=VPN");
        Assert.Equal(HttpStatusCode.OK, independent.StatusCode);
        using var foreign = await bob.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var allowed = await alice.GetAsync($"/api/incidents/{DbSeeder.AliceIncidentId}");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        await Task.Delay(TimeSpan.FromSeconds(3.5));
        using var recovered = await alice.GetAsync("/api/incidents/search?q=VPN");
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

    [Fact]
    public async Task REG_RATE_SEARCH_AnonymousHeaderCannotResetQuota()
    {
        using var host = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.PostConfigure<SearchRateLimitOptions>(o => { o.PermitLimit = 1; o.WindowSeconds = 60; })));
        using var first = host.CreateClient();
        using var second = host.CreateClient();
        using var ok = await first.GetAsync("/api/incidents/search?q=VPN");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        second.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2.101");
        using var blocked = await second.GetAsync("/api/incidents/search?q=other&client=fake");
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        using var health = await second.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task Production_HeadersOnFiveResponseKinds_AndResetForbidden()
    {
        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__SecureLab");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SecureLab", factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("SecureLab"));
            using var production = factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
            Assert.Equal("Production", production.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
            using var client = await production.LoginSeedAsync("alice");
            await client.UseAntiforgeryTokenAsync();
            using var html = await client.GetAsync("/");
            using var api = await client.GetAsync("/api/me");
            using var token = await client.GetAsync(AntiforgeryFilter.TokenRoute);
            using var invalid = await client.PostAsJsonAsync("/api/incidents", new { title = "", description = "" });
            using var missing = await client.GetAsync("/api/incidents/ffffffff-ffff-ffff-ffff-ffffffffffff");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.True((await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("title", out _));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            foreach (var response in new[] { html, api, token, invalid, missing })
            {
                Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
                Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
                Assert.Contains("script-src 'self'", response.Headers.GetValues("Content-Security-Policy").Single());
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseBootstrap.InitializeAsync(
                production.Services, production.Services.GetRequiredService<IConfiguration>(),
                production.Services.GetRequiredService<IHostEnvironment>(), true));
        }
        finally { Environment.SetEnvironmentVariable("ConnectionStrings__SecureLab", previous); }
    }
}
