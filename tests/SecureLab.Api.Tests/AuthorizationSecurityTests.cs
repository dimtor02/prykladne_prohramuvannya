using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureLab.Api.Application.Incidents;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;
using Xunit.Abstractions;

namespace SecureLab.Api.Tests;

// Real Identity login/cookie and PostgreSQL: no fake role headers or in-memory auth bypass.
public sealed class AuthorizationSecurityTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly SecureLabApiFactory factory = new();
    private static readonly Guid PublicAliceComment = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid BobComment = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid InternalComment = Guid.Parse("30000000-0000-0000-0000-000000000003");
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => factory.DisposeAsync().AsTask();

    private async Task<string> Snapshot(Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        var item = await db.Incidents.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.OwnerUserId, x.Title, x.Description, x.Severity, x.Status,
            x.OccurredAtUtc, x.CreatedAtUtc, x.UpdatedAtUtc,
            Comments = x.Comments.OrderBy(c => c.Id).Select(c => new { c.Id, c.Text, c.IsInternal }).ToList(),
            History = x.StatusHistory.OrderBy(h => h.Id)
                .Select(h => new { h.Id, h.ChangedByUserId, h.OldStatus, h.NewStatus, h.Note }).ToList()
        }).SingleAsync();
        return JsonSerializer.Serialize(item);
    }

    private async Task Cleanup(Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SecureLabDbContext>().Incidents
            .Where(x => x.Id == id).ExecuteDeleteAsync();
    }

    private static async Task<CreatedIncidentResponse> Create(HttpClient owner)
    {
        using var response = await owner.PostAsJsonAsync("/api/incidents", new
        {
            title = $"LR04-{Guid.NewGuid():N}", description = "Synthetic authorization regression fixture.",
            severity = "Low", occurredAtUtc = DateTimeOffset.UtcNow
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedIncidentResponse>())!;
    }

    private static async Task<JsonObject> Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Null(response.Headers.Location);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        Assert.Equal((int)status, problem["status"]!.GetValue<int>());
        return problem;
    }

    [Theory]
    [InlineData("GET", "/api/incidents")]
    [InlineData("GET", "/api/incidents/20000000-0000-0000-0000-000000000001")]
    [InlineData("PUT", "/api/incidents/20000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/incidents/20000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/incidents/20000000-0000-0000-0000-000000000003/triage")]
    [InlineData("GET", "/api/incident-comments/30000000-0000-0000-0000-000000000001")]
    public async Task A01_AnonymousProtectedRequests_Return401WithoutRedirect(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT") request.Content = JsonContent.Create(new { title = "forged", description = "forged" });
        var before = await Snapshot(DbSeeder.AliceIncidentId);
        using var response = await client.SendAsync(request);
        await Problem(response, HttpStatusCode.Unauthorized);
        Assert.Equal(before, await Snapshot(DbSeeder.AliceIncidentId));
        output.WriteLine($"A-01 {method} {path}: 401, no redirect, seed unchanged");
    }

    [Theory]
    [InlineData("alice", "20000000-0000-0000-0000-000000000001", 200)]
    [InlineData("alice", "20000000-0000-0000-0000-000000000002", 404)]
    [InlineData("bob", "20000000-0000-0000-0000-000000000001", 404)]
    [InlineData("bob", "20000000-0000-0000-0000-000000000002", 200)]
    [InlineData("morgan", "20000000-0000-0000-0000-000000000001", 200)]
    [InlineData("morgan", "20000000-0000-0000-0000-000000000002", 200)]
    [InlineData("admin", "20000000-0000-0000-0000-000000000001", 404)]
    public async Task A02_A03_A03R_DetailsFollowOwnerAndRoleMatrix(string user, string id, int expected)
    {
        using var client = await factory.LoginSeedAsync(user);
        client.DefaultRequestHeaders.Add("X-Demo-UserId", DbSeeder.MorganId.ToString());
        client.DefaultRequestHeaders.Add("X-Role", "Analyst");
        using var response = await client.GetAsync($"/api/incidents/{id}");
        Assert.Equal(expected, (int)response.StatusCode);
        if (expected == 200)
        {
            var details = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            Assert.Equal(Guid.Parse(id), details["id"]!.GetValue<Guid>());
            Assert.Null(details["ownerUserId"]);
            Assert.Null(details["email"]);
        }
        else
        {
            var problem = await Problem(response, HttpStatusCode.NotFound);
            Assert.Null(problem["description"]);
            Assert.DoesNotContain("Невідома спроба входу", problem.ToJsonString());
        }
        output.WriteLine($"A-02/A-03/A-03R {user} GET {id}: {expected}; forged headers have no authority");
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("POST")]
    public async Task A04_NonOwnerMutation_Returns404_AndLeavesWholeResourceUnchanged(string method)
    {
        using var alice = await factory.LoginSeedAsync("alice");
        var before = await Snapshot(DbSeeder.BobIncidentId);
        var path = $"/api/incidents/{DbSeeder.BobIncidentId}" + (method == "POST" ? "/triage" : "");
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method != "DELETE") request.Content = JsonContent.Create(new
        {
            title = "forged", description = "Attempted overwrite of Bob's incident content.",
            role = "Analyst", ownerUserId = DbSeeder.AliceId, status = "Triaged"
        });
        using var response = await alice.SendAsync(request);
        await Problem(response, HttpStatusCode.NotFound);
        Assert.Equal(before, await Snapshot(DbSeeder.BobIncidentId));
        output.WriteLine($"A-04 Alice {method} Bob: 404; all scalar fields/comments/history unchanged");
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("bob")]
    [InlineData("morgan")]
    [InlineData("admin")]
    public async Task A05_ListContainsExactlyTheAllowedSet(string user)
    {
        using var client = await factory.LoginSeedAsync(user);
        var rows = (await client.GetFromJsonAsync<List<IncidentListItemResponse>>("/api/incidents"))!;
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        var expected = await db.Incidents.AsNoTracking()
            .Where(x => user == "morgan" || (user == "alice" && x.OwnerUserId == DbSeeder.AliceId)
                || (user == "bob" && x.OwnerUserId == DbSeeder.BobId)).Select(x => x.Id).ToListAsync();
        Assert.Equal(expected.OrderBy(x => x), rows.Select(x => x.Id).OrderBy(x => x));
        output.WriteLine($"A-05 {user}: exact authorized set, {rows.Count} rows");
    }

    [Fact]
    public async Task A06_ReporterTriage_Returns403Forbidden_AndDoesNotChangeStatus()
    {
        using var alice = await factory.LoginSeedAsync("alice");
        var before = await Snapshot(DbSeeder.SafeTextIncidentId);
        using var response = await alice.PostAsJsonAsync($"/api/incidents/{DbSeeder.SafeTextIncidentId}/triage",
            new { role = "Analyst", status = "Triaged", isTransitionAllowed = true });
        var problem = await Problem(response, HttpStatusCode.Forbidden);
        Assert.Equal("Forbidden", problem["title"]!.GetValue<string>());
        Assert.Equal(before, await Snapshot(DbSeeder.SafeTextIncidentId));
        output.WriteLine("A-06 Reporter Alice triage own New: 403 Forbidden; status/content/history unchanged");
    }

    [Fact]
    public async Task A07_AnalystCanOnlyTriageNew_AndHistoryUsesVerifiedActor()
    {
        using var alice = await factory.LoginSeedAsync("alice");
        using var morgan = await factory.LoginSeedAsync("morgan");
        var item = await Create(alice);
        try
        {
            using var allowed = await morgan.PostAsJsonAsync($"/api/incidents/{item.Id}/triage",
                new { status = "Closed", ownerUserId = DbSeeder.BobId, role = "Administrator" });
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
                var entity = await db.Incidents.AsNoTracking().SingleAsync(x => x.Id == item.Id);
                Assert.Equal(IncidentStatus.Triaged, entity.Status);
                Assert.Equal(DbSeeder.AliceId, entity.OwnerUserId);
                Assert.Equal(item.Title, entity.Title);
                var history = await db.IncidentStatusHistory.SingleAsync(x => x.IncidentId == item.Id);
                Assert.Equal(DbSeeder.MorganId, history.ChangedByUserId);
                Assert.Equal(IncidentStatus.New, history.OldStatus);
                Assert.Equal(IncidentStatus.Triaged, history.NewStatus);
            }
            var before = await Snapshot(item.Id);
            using var repeat = await morgan.PostAsync($"/api/incidents/{item.Id}/triage", null);
            await Problem(repeat, HttpStatusCode.Conflict);
            Assert.Equal(before, await Snapshot(item.Id));
            output.WriteLine("A-07 Morgan: New->Triaged 204; forged Closed ignored; actor Morgan; repeat 409 unchanged");
        }
        finally { await Cleanup(item.Id); }
    }

    [Fact]
    public async Task A08a_OwnerCanUpdateContent_ButForgedOwnerRoleStatusSeverityAreIgnored()
    {
        using var alice = await factory.LoginSeedAsync("alice");
        var item = await Create(alice);
        try
        {
            // Compare persisted timestamps; PostgreSQL stores microseconds, .NET can produce 100ns ticks.
            using var baseline = JsonDocument.Parse(await Snapshot(item.Id));
            using var update = await alice.PutAsJsonAsync($"/api/incidents/{item.Id}", new
            {
                title = item.Title + " edited", description = "Edited permitted content.",
                ownerUserId = DbSeeder.BobId, role = "Analyst", status = "Closed", severity = "Critical",
                occurredAtUtc = "2030-01-01T00:00:00Z", id = DbSeeder.BobIncidentId
            });
            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            var entity = await db.Incidents.AsNoTracking().SingleAsync(x => x.Id == item.Id);
            Assert.Equal(item.Title + " edited", entity.Title);
            Assert.Equal("Edited permitted content.", entity.Description);
            Assert.Equal(DbSeeder.AliceId, entity.OwnerUserId);
            Assert.Equal(IncidentStatus.New, entity.Status);
            Assert.Equal(IncidentSeverity.Low, entity.Severity);
            Assert.Equal(baseline.RootElement.GetProperty("CreatedAtUtc").GetDateTimeOffset(), entity.CreatedAtUtc);
            Assert.Equal(baseline.RootElement.GetProperty("OccurredAtUtc").GetDateTimeOffset(), entity.OccurredAtUtc);
            var roles = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
                where ur.UserId == DbSeeder.AliceId select r.Name).ToListAsync();
            Assert.Equal(new[] { DbSeeder.ReporterRole }, roles);
            Assert.False(await db.IncidentStatusHistory.AnyAsync(x => x.IncidentId == item.Id));
            using var triage = await alice.PostAsync($"/api/incidents/{item.Id}/triage", null);
            await Problem(triage, HttpStatusCode.Forbidden);
            output.WriteLine("A-08a PUT 204; content changed; owner/role/status/severity/id/times preserved; triage still 403");
        }
        finally { await Cleanup(item.Id); }
    }

    [Fact]
    public async Task OwnerCanDeleteOwnFixture_AndChildrenCascade()
    {
        using var alice = await factory.LoginSeedAsync("alice");
        var item = await Create(alice);
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
                db.IncidentComments.Add(new IncidentComment
                {
                    Id = Guid.NewGuid(), IncidentId = item.Id, AuthorUserId = DbSeeder.MorganId,
                    Text = "Synthetic child", CreatedAtUtc = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }
            using var response = await alice.DeleteAsync($"/api/incidents/{item.Id}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await using var check = factory.Services.CreateAsyncScope();
            var checkDb = check.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            Assert.False(await checkDb.Incidents.AnyAsync(x => x.Id == item.Id));
            Assert.False(await checkDb.IncidentComments.AnyAsync(x => x.IncidentId == item.Id));
        }
        finally { await Cleanup(item.Id); }
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task AnalystReadPermissionDoesNotGrantContentMutation(string method)
    {
        using var morgan = await factory.LoginSeedAsync("morgan");
        var before = await Snapshot(DbSeeder.AliceIncidentId);
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/incidents/{DbSeeder.AliceIncidentId}");
        if (method == "PUT") request.Content = JsonContent.Create(new { title = "forged", description = "forged" });
        using var response = await morgan.SendAsync(request);
        await Problem(response, HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot(DbSeeder.AliceIncidentId));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("POST")]
    [InlineData("CHILD")]
    public async Task A10_ForeignAndUnknownIds_HaveIdentical404Responses(string method)
    {
        using var alice = await factory.LoginSeedAsync("alice");
        var foreignId = method == "CHILD" ? BobComment : DbSeeder.BobIncidentId;
        var unknownId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        async Task<(JsonObject Body, string? ContentType)> Send(Guid id)
        {
            var path = method == "CHILD" ? $"/api/incident-comments/{id}" : $"/api/incidents/{id}";
            if (method == "POST") path += "/triage";
            using var request = new HttpRequestMessage(new HttpMethod(method == "CHILD" ? "GET" : method), path);
            if (method == "PUT") request.Content = JsonContent.Create(new { title = "forged", description = "forged" });
            using var response = await alice.SendAsync(request);
            var problem = await Problem(response, HttpStatusCode.NotFound);
            problem.Remove("traceId");
            problem.Remove("requestId");
            return (problem, response.Content.Headers.ContentType?.ToString());
        }
        var foreign = await Send(foreignId);
        var unknown = await Send(unknownId);
        Assert.Equal(foreign.ContentType, unknown.ContentType);
        Assert.True(JsonNode.DeepEquals(foreign.Body, unknown.Body));
        output.WriteLine($"A-10 {method} foreign={foreign.Body.ToJsonString()}; unknown={unknown.Body.ToJsonString()}; Content-Type={foreign.ContentType}");
    }

    [Fact]
    public async Task A12_ChildChecksParent_AndInternalCommentIsHiddenFromReporterOwner()
    {
        using var alice = await factory.LoginSeedAsync("alice");
        using var morgan = await factory.LoginSeedAsync("morgan");
        using var publicOwn = await alice.GetAsync($"/api/incident-comments/{PublicAliceComment}");
        Assert.Equal(HttpStatusCode.OK, publicOwn.StatusCode);
        using var foreign = await alice.GetAsync($"/api/incident-comments/{BobComment}");
        await Problem(foreign, HttpStatusCode.NotFound);
        using var internalOwn = await alice.GetAsync($"/api/incident-comments/{InternalComment}");
        await Problem(internalOwn, HttpStatusCode.NotFound);
        var details = (await alice.GetFromJsonAsync<IncidentDetailsResponse>($"/api/incidents/{DbSeeder.AliceIncidentId}"))!;
        Assert.DoesNotContain(details.Comments, c => c.Id == InternalComment);
        using var analystInternal = await morgan.GetAsync($"/api/incident-comments/{InternalComment}");
        Assert.Equal(HttpStatusCode.OK, analystInternal.StatusCode);
        var analystDetails = (await morgan.GetFromJsonAsync<IncidentDetailsResponse>($"/api/incidents/{DbSeeder.AliceIncidentId}"))!;
        Assert.Contains(analystDetails.Comments, c => c.Id == InternalComment);
        using var analystForeignChild = await morgan.GetAsync($"/api/incident-comments/{BobComment}");
        Assert.Equal(HttpStatusCode.OK, analystForeignChild.StatusCode);
        output.WriteLine("A-12 Reporter public own 200; foreign/internal 404; internal absent from details; Analyst internal/details/foreign child 200");
    }

    [Fact]
    public async Task A09_DeniedPrivilegeAudit_ContainsOnlySafeFields_AndMatchingResponseTrace()
    {
        using var provider = new AuditProvider();
        await using var host = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logs => logs.AddProvider(provider)));
        using var alice = await host.LoginSeedAsync("alice");
        using var response = await alice.PostAsJsonAsync($"/api/incidents/{DbSeeder.SafeTextIncidentId}/triage",
            new { password = "do-not-log-this-synthetic-body", role = "Analyst" });
        var problem = await Problem(response, HttpStatusCode.Forbidden);
        var entry = Assert.Single(provider.Events);
        Assert.Equal("authorization.denied", entry["EventName"]);
        Assert.Equal(DbSeeder.AliceId, entry["SubjectId"]);
        Assert.Equal("incident.triage", entry["Action"]);
        Assert.Equal(DbSeeder.SafeTextIncidentId, entry["ResourceId"]);
        Assert.Equal("denied", entry["Result"]);
        Assert.Equal(problem["traceId"]!.GetValue<string>(), entry["TraceId"]);
        Assert.IsType<DateTimeOffset>(entry["OccurredAtUtc"]);
        Assert.Equal(new[] { "Action", "EventName", "OccurredAtUtc", "ResourceId", "Result", "SubjectId", "TraceId", "{OriginalFormat}" },
            entry.Keys.OrderBy(x => x, StringComparer.Ordinal));
        var text = JsonSerializer.Serialize(entry);
        Assert.DoesNotContain("do-not-log-this-synthetic-body", text);
        Assert.DoesNotContain(Environment.GetEnvironmentVariable("SeedUsers__Password")!, text);
        Assert.DoesNotContain("cookie", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
        output.WriteLine("A-09 safe structured event, response trace matches: " + text);
    }

    private sealed class AuditProvider : ILoggerProvider
    {
        public ConcurrentQueue<Dictionary<string, object?>> Events { get; } = new();
        public ILogger CreateLogger(string categoryName) => new AuditLogger(categoryName, Events);
        public void Dispose() { }
    }

    private sealed class AuditLogger(string category, ConcurrentQueue<Dictionary<string, object?>> events) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error,
            Func<TState, Exception?, string> formatter)
        {
            if (category == typeof(IncidentAccessPolicy).FullName
                && state is IEnumerable<KeyValuePair<string, object?>> fields)
                events.Enqueue(fields.ToDictionary(x => x.Key, x => x.Value));
        }
    }
}
