using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;
using SecureLab.Api.Scaffolding;
using Xunit.Abstractions;

namespace SecureLab.Api.Tests;

// A new host per case prevents another scenario's login quota from changing the result.
public sealed class AuthenticationSecurityTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly SecureLabApiFactory factory = new();
    private static string SeedPassword => Environment.GetEnvironmentVariable("SeedUsers__Password")
        ?? throw new InvalidOperationException("Set local SeedUsers__Password outside Git.");
    private static string NewPassword() => "Aa1!" + Guid.NewGuid().ToString("N");
    private static HttpClient Client(WebApplicationFactory<Program> host) => host.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
        });
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => factory.DisposeAsync().AsTask();

    private static void SafeProperties(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
            foreach (var property in node.EnumerateObject())
            {
                Assert.False(new[] { "password", "confirmPassword", "passwordHash", "securityStamp",
                    "concurrencyStamp", "cookie", "token", "accessToken", "refreshToken" }
                    .Contains(property.Name, StringComparer.OrdinalIgnoreCase));
                SafeProperties(property.Value);
            }
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var value in node.EnumerateArray()) SafeProperties(value);
    }

    private async Task<JsonDocument> Problem(HttpResponseMessage response, HttpStatusCode status, string id)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.Location);
        Assert.Empty(response.Headers.WwwAuthenticate);
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(text);
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("title").GetString()));
        // ValidationProblem legitimately has an errors.password key, but never a supplied value.
        var seedPassword = Environment.GetEnvironmentVariable("SeedUsers__Password");
        if (!string.IsNullOrEmpty(seedPassword)) Assert.False(text.Contains(seedPassword, StringComparison.Ordinal));
        Assert.DoesNotContain("stackTrace", text, StringComparison.OrdinalIgnoreCase);
        output.WriteLine($"EVIDENCE {id}: HTTP {(int)status}, application/problem+json, no redirect/bearer challenge");
        return json;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A01_AnonymousProfileAndCreate_Return401_EvenWithForgedHeader(bool forgeHeader)
    {
        using var client = Client(factory);
        if (forgeHeader) client.DefaultRequestHeaders.Add("X-Demo-UserId", DbSeeder.BobId.ToString());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        var count = await db.Incidents.CountAsync();
        using var me = await client.GetAsync("/api/me");
        using var meProblem = await Problem(me, HttpStatusCode.Unauthorized, "A-01/me");
        using var create = await client.PostAsJsonAsync("/api/incidents", new
        {
            title = "anonymous", description = "synthetic", severity = "Low", occurredAtUtc = DateTimeOffset.UtcNow
        });
        using var createProblem = await Problem(create, HttpStatusCode.Unauthorized, "A-01/create");
        Assert.Equal(count, await db.Incidents.CountAsync());
    }

    [Fact]
    public async Task A02_LoginThenMe_ReturnsOnlySafeAliceProfile()
    {
        using var client = await factory.LoginSeedAsync("alice");
        using var response = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        SafeProperties(json.RootElement);
        Assert.Equal(new[] { "displayName", "id", "userName" },
            json.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(DbSeeder.AliceId, json.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("alice", json.RootElement.GetProperty("userName").GetString());
        Assert.Equal("Аліса Коваль", json.RootElement.GetProperty("displayName").GetString());
        output.WriteLine("EVIDENCE A-02: login 204; me 200; Alice id; exactly id/userName/displayName");
    }

    [Fact]
    public async Task A03_ForeignHeaderCannotReplaceAuthenticatedAlice()
    {
        using var client = await factory.LoginSeedAsync("alice");
        var before = await client.GetFromJsonAsync<UserProfileResponse>("/api/me");
        client.DefaultRequestHeaders.Add("X-Demo-UserId", DbSeeder.BobId.ToString());
        var after = await client.GetFromJsonAsync<UserProfileResponse>("/api/me");
        Assert.NotNull(before);
        Assert.Equal(DbSeeder.AliceId, before.Id);
        Assert.Equal(before, after);
        output.WriteLine("EVIDENCE A-03: Bob header with Alice cookie; unchanged Alice profile");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A04_A05_BobOwnsNewIncident_WithOrWithoutForgedAliceOwner(bool forgeOwner)
    {
        var marker = "Lr03-" + Guid.NewGuid().ToString("N");
        using var client = await factory.LoginSeedAsync("bob");
        client.DefaultRequestHeaders.Add("X-Demo-UserId", DbSeeder.AliceId.ToString());
        var body = new Dictionary<string, object?>
        {
            ["title"] = marker, ["description"] = "Штучний інцидент перевірки власника.",
            ["severity"] = "Low", ["occurredAtUtc"] = DateTimeOffset.UtcNow
        };
        if (forgeOwner) body["ownerUserId"] = DbSeeder.AliceId;
        try
        {
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            SafeProperties(json.RootElement);
            var id = json.RootElement.GetProperty("id").GetGuid();
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            var saved = await db.Incidents.AsNoTracking().SingleAsync(item => item.Id == id);
            Assert.Equal(DbSeeder.BobId, saved.OwnerUserId);
            output.WriteLine($"EVIDENCE {(forgeOwner ? "A-05" : "A-04")}: HTTP 201; stored owner=Bob; forged header ignored");
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<SecureLabDbContext>().Incidents
                .Where(item => item.Title == marker).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task A06_LogoutDeletesMainCookie_AndSameClientLosesAccess()
    {
        using var client = await factory.LoginSeedAsync("alice");
        using var before = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var mainCookies = logout.Headers.GetValues("Set-Cookie")
            .Where(value => value.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal)).ToArray();
        Assert.True(mainCookies.Length >= 1);
        // Never include the name=value part in assertion messages.
        var attributes = mainCookies[0].Split(';').Skip(1).Select(value => value.Trim()).ToArray();
        Assert.Contains(attributes, value => value.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));
        using var after = await client.GetAsync("/api/me");
        using var problem = await Problem(after, HttpStatusCode.Unauthorized, "A-06");
        using var create = await client.PostAsJsonAsync("/api/incidents", new { title = "after logout" });
        using var createProblem = await Problem(create, HttpStatusCode.Unauthorized, "A-06/create");
    }

    [Fact]
    public async Task A07_UnknownUserAndWrongPassword_HaveIdenticalStableProblemShape()
    {
        using var client = Client(factory);
        var wrongPassword = NewPassword();
        using var unknown = await client.PostAsJsonAsync("/api/auth/login", new
            { userName = "missing" + Guid.NewGuid().ToString("N"), password = wrongPassword });
        using var wrong = await client.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = wrongPassword });
        using var first = await Problem(unknown, HttpStatusCode.Unauthorized, "A-07/unknown");
        using var second = await Problem(wrong, HttpStatusCode.Unauthorized, "A-07/wrong");
        Assert.Equal(unknown.Content.Headers.ContentType?.ToString(), wrong.Content.Headers.ContentType?.ToString());
        foreach (var field in new[] { "type", "title", "status", "detail", "code", "message" })
        {
            var hasFirst = first.RootElement.TryGetProperty(field, out var one);
            var hasSecond = second.RootElement.TryGetProperty(field, out var two);
            Assert.Equal(hasFirst, hasSecond);
            if (hasFirst) Assert.Equal(one.GetRawText(), two.GetRawText());
        }
        foreach (var json in new[] { first, second })
            if (json.RootElement.TryGetProperty("traceId", out var trace))
                Assert.False(string.IsNullOrWhiteSpace(trace.GetString()));
    }

    [Fact]
    public async Task T01_Duplicate_OverpostingAndPasswordWhitespace_RespectRegistrationContract()
    {
        var name = "reg" + Guid.NewGuid().ToString("N");
        var email = name + "@example.test";
        var password = " " + NewPassword() + " ";
        var suppliedId = Guid.NewGuid();
        using var client = Client(factory);
        var body = new { userName = " " + name + " ", email = " " + email + " ",
            displayName = " Студент ЛР3 ", password, id = suppliedId, role = "Administrator", passwordHash = "ignored" };
        try
        {
            using var registration = await client.PostAsJsonAsync("/api/auth/register", body);
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            Assert.Equal("/api/me", registration.Headers.Location?.OriginalString);
            using var json = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
            SafeProperties(json.RootElement);
            Assert.Equal(3, json.RootElement.EnumerateObject().Count());
            var id = json.RootElement.GetProperty("id").GetGuid();
            Assert.NotEqual(suppliedId, id);
            Assert.Equal(name, json.RootElement.GetProperty("userName").GetString());
            Assert.Equal("Студент ЛР3", json.RootElement.GetProperty("displayName").GetString());
            await using var scope = factory.Services.CreateAsyncScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(id.ToString());
            Assert.NotNull(user);
            Assert.Equal(new[] { DbSeeder.ReporterRole }, (await users.GetRolesAsync(user)).ToArray());
            Assert.True(await users.CheckPasswordAsync(user, password));
            Assert.False(await users.CheckPasswordAsync(user, password.Trim()));
            Assert.True(!string.IsNullOrWhiteSpace(user.PasswordHash) && user.PasswordHash != password);
            using var session = await factory.LoginAsync(name, password);
            var profile = await session.GetFromJsonAsync<UserProfileResponse>("/api/me");
            Assert.NotNull(profile);
            Assert.Equal(id, profile.Id);
            using var repeat = await client.PostAsJsonAsync("/api/auth/register", body);
            using var repeatProblem = await Problem(repeat, HttpStatusCode.Conflict, "Duplicate/both");
            using var nameOnly = await client.PostAsJsonAsync("/api/auth/register",
                new { userName = name.ToUpperInvariant(), email = "other" + email, displayName = "Інший", password });
            using var nameProblem = await Problem(nameOnly, HttpStatusCode.Conflict, "Duplicate/name-case");
            using var emailOnly = await client.PostAsJsonAsync("/api/auth/register",
                new { userName = "other" + name, email = email.ToUpperInvariant(), displayName = "Інший", password });
            using var emailProblem = await Problem(emailOnly, HttpStatusCode.Conflict, "Duplicate/email-case");
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SecureLabDbContext>()
                .Users.CountAsync(item => item.NormalizedEmail == email.ToUpperInvariant()));
            output.WriteLine("EVIDENCE T-01/T-03: safe 201; Reporter only; server id; standard password verifier; password whitespace preserved; duplicates 409");
        }
        finally { await DeleteUser(name); }
    }

    [Theory]
    [InlineData("missing", "userName")]
    [InlineData("long-name", "userName")]
    [InlineData("bad-name", "userName")]
    [InlineData("bad-email", "email")]
    [InlineData("long-email", "email")]
    [InlineData("blank-display", "displayName")]
    [InlineData("long-display", "displayName")]
    [InlineData("control-display", "displayName")]
    [InlineData("short-password", "password")]
    [InlineData("long-password", "password")]
    [InlineData("weak-password", "password")]
    public async Task T02_InvalidRegister_ReturnsSafe400_WithoutCreatingUser(string scenario, string field)
    {
        var name = "reg" + Guid.NewGuid().ToString("N");
        var body = new Dictionary<string, object?>
        {
            ["userName"] = name, ["email"] = name + "@example.test", ["displayName"] = "Студент", ["password"] = NewPassword()
        };
        switch (scenario)
        {
            case "missing": body.Remove("userName"); break;
            case "long-name": body["userName"] = name.PadRight(257); break;
            case "bad-name": body["userName"] = name + "/"; break;
            case "bad-email": body["email"] = "invalid"; break;
            case "long-email": body["email"] = new string('x', 257) + "@example.test"; break;
            case "blank-display": body["displayName"] = "   "; break;
            case "long-display": body["displayName"] = new string('x', 121); break;
            case "control-display": body["displayName"] = "bad\u0001name"; break;
            case "short-password": body["password"] = "x"; break;
            case "long-password": body["password"] = new string('x', 129); break;
            case "weak-password": body["password"] = new string('x', 12); break;
        }
        using var client = Client(factory);
        using var response = await client.PostAsJsonAsync("/api/auth/register", body);
        using var json = await Problem(response, HttpStatusCode.BadRequest, "T-02/" + scenario);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>().Users;
        Assert.False(await users.AnyAsync(user => user.Email == name + "@example.test" || user.UserName == name));
    }

    [Fact]
    public async Task CookieFlags_HttpsAndProduction_UseSecureHttpOnlyLaxSessionCookie()
    {
        var name = "flags" + Guid.NewGuid().ToString("N");
        var password = NewPassword();
        using var registrationClient = Client(factory);
        using var registration = await registrationClient.PostAsJsonAsync("/api/auth/register", new
        {
            userName = name, email = name + "@example.test", displayName = "Cookie flags test", password
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        try
        {
            // Host settings are available before Program reads configuration; late application
            // configuration callbacks cannot supply an early connection-string lookup.
            var connection = factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("SecureLab")!;
            using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")
                .UseSetting("ConnectionStrings:SecureLab", connection)
                .UseSetting("Database:ApplyMigrationsOnStartup", "false"));
            foreach (var host in new WebApplicationFactory<Program>[] { factory, production })
            {
                using var client = Client(host);
                using var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = name, password });
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                var cookies = response.Headers.GetValues("Set-Cookie")
                    .Where(value => value.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal)).ToArray();
                Assert.True(cookies.Length == 1);
                var attributes = cookies[0].Split(';').Skip(1).Select(value => value.Trim().ToLowerInvariant()).ToArray();
                Assert.Contains("httponly", attributes);
                Assert.Contains("secure", attributes);
                Assert.Contains("samesite=lax", attributes);
                Assert.Contains("path=/", attributes);
                Assert.DoesNotContain(attributes, value => value.StartsWith("expires=") || value.StartsWith("max-age="));
                using var me = await client.GetAsync("/api/me");
                Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            }
            output.WriteLine("EVIDENCE Cookie flags: Development HTTPS and Production HTTPS; Secure, HttpOnly, SameSite=Lax, Path=/, session cookie");
        }
        finally { await DeleteUser(name); }
    }

    [Fact]
    public async Task LoginRateLimit_TransportIpQuota_RecoversAndIgnoresForwardedHeader()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<LoginRateLimitOptions>(settings => { settings.PermitLimit = 3; settings.WindowSeconds = 3; })));
        using var first = Client(host);
        using var second = Client(host);
        var wrongPassword = NewPassword();
        for (var index = 0; index < 3; index++)
        {
            using var reply = await first.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = wrongPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, reply.StatusCode);
        }
        second.DefaultRequestHeaders.Add("X-Forwarded-For", "192.0.2.123");
        using var rejected = await second.PostAsJsonAsync("/api/auth/login", new { userName = "bob", password = wrongPassword });
        using var problem = await Problem(rejected, HttpStatusCode.TooManyRequests, "Rate limit");
        Assert.NotNull(rejected.Headers.RetryAfter);
        using var unrelated = await second.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, unrelated.StatusCode);
        using var fresh = factory.WithWebHostBuilder(_ => { });
        using var separateClient = Client(fresh);
        using var independent = await separateClient.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = wrongPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, independent.StatusCode);
        await Task.Delay(TimeSpan.FromSeconds(3.4));
        using var recovered = await second.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = SeedPassword });
        Assert.Equal(HttpStatusCode.NoContent, recovered.StatusCode);
        output.WriteLine("EVIDENCE Rate limit: real login policy, isolated test options 3/3s; 401x3 -> 429; another client/name/forwarded header shares quota; health 200; new host independent; after window login 204");
    }

    [Fact]
    public async Task S01_AuthenticationEvents_ContainNoCredentialsOrRequestBody()
    {
        var capture = new AuthLogCapture();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(capture)));
        using var client = Client(host);
        var wrongPassword = NewPassword();
        using var failed = await client.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = wrongPassword });
        using var success = await client.PostAsJsonAsync("/api/auth/login", new { userName = "alice", password = SeedPassword });
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, success.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        var events = capture.Messages.ToArray();
        Assert.Equal(3, events.Length);
        Assert.Contains(events, value => value.Contains("result failure"));
        Assert.Contains(events, value => value.Contains("result success"));
        foreach (var entry in events)
        {
            Assert.False(entry.Contains(SeedPassword, StringComparison.Ordinal));
            Assert.False(entry.Contains(wrongPassword, StringComparison.Ordinal));
            foreach (var excluded in new[] { "password", "cookie", "token", "Authorization", "alice", "@example.test" })
                Assert.False(entry.Contains(excluded, StringComparison.OrdinalIgnoreCase));
            Assert.Contains("trace", entry);
            Assert.Contains("subject", entry);
        }
        output.WriteLine("EVIDENCE S-01/events: failure, success, logout; only event/result/subject/trace in auth messages; no password/cookie/hash/token/raw body/user name");
    }

    [Fact]
    public async Task LoginRateLimit_DefaultPolicy_AllowsTenAttemptsThen429()
    {
        using var client = Client(factory);
        var password = NewPassword();
        var name = "missing" + Guid.NewGuid().ToString("N");
        for (var i = 0; i < 10; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = name, password });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        using var denied = await client.PostAsJsonAsync("/api/auth/login", new { userName = name, password });
        using var problem = await Problem(denied, HttpStatusCode.TooManyRequests, "Default quota");
        output.WriteLine("EVIDENCE Rate limit/default: configured 10 attempts/60s; attempts 1-10=401, 11=429");
    }

    [Fact]
    public async Task T03_SeedIdentityModel_KeepsGuidsRolesOwnersAndStandardSaltedHashes()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var rows = new[]
        {
            (DbSeeder.AliceId, "alice", DbSeeder.ReporterRole),
            (DbSeeder.BobId, "bob", DbSeeder.ReporterRole),
            (DbSeeder.MorganId, "morgan", DbSeeder.AnalystRole),
            (DbSeeder.AdminId, "admin", DbSeeder.AdministratorRole)
        };
        var hashes = new HashSet<string>();
        foreach (var (id, name, role) in rows)
        {
            var user = await users.FindByIdAsync(id.ToString());
            Assert.NotNull(user);
            Assert.Equal(name, user.UserName);
            Assert.True(await users.IsInRoleAsync(user, role));
            Assert.True(await users.CheckPasswordAsync(user, SeedPassword));
            Assert.True(!string.IsNullOrWhiteSpace(user.PasswordHash) && user.PasswordHash != SeedPassword);
            Assert.True(hashes.Add(user.PasswordHash!));
        }
        Assert.Equal(DbSeeder.AliceId, (await db.Incidents.SingleAsync(item => item.Id == DbSeeder.AliceIncidentId)).OwnerUserId);
        Assert.Equal(DbSeeder.BobId, (await db.Incidents.SingleAsync(item => item.Id == DbSeeder.BobIncidentId)).OwnerUserId);
        Assert.DoesNotContain(db.Model.GetEntityTypes(), entity => entity.ClrType.Name == "StudyUser");
        var identity = db.Model.FindEntityType(typeof(ApplicationUser));
        Assert.NotNull(identity);
        Assert.Equal("identity_users", identity.GetTableName());
        Assert.NotNull(identity.FindProperty("PasswordHash"));
        Assert.Null(identity.FindProperty("Password"));
        output.WriteLine("EVIDENCE T-03/seed: four stable GUIDs/roles; Alice/Bob incident owners preserved; single Identity store; PasswordHash column, no Password column; four distinct salted representations; standard verifier accepts seed password; no hash output");
    }

    private async Task DeleteUser(string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByNameAsync(name);
        if (user is not null) Assert.True((await users.DeleteAsync(user)).Succeeded);
    }

    private sealed class AuthLogCapture : ILoggerProvider
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);
        public void Dispose() { }
        private sealed class CaptureLogger(AuthLogCapture owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;
            public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (category == typeof(Lab03Identity).FullName) owner.Messages.Enqueue(formatter(state, exception));
            }
        }
    }
}
