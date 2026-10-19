using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;
using Xunit.Abstractions;

namespace SecureLab.Api.Tests;

public sealed class IncidentCreationTests(SecureLabApiFactory factory, ITestOutputHelper output)
    : IClassFixture<SecureLabApiFactory>
{
    private static Dictionary<string, object?> ValidBody(string marker) => new()
    {
        ["title"] = marker,
        ["description"] = $"Штучний опис інциденту для тесту {marker}.",
        ["severity"] = "Low",
        ["occurredAtUtc"] = "2026-09-01T13:00:00+03:00"
    };

    private async Task CleanupAsync(string marker)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        await db.Incidents.Where(item => item.Title.Contains(marker) || item.Description.Contains(marker))
            .ExecuteDeleteAsync();
    }

    private async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode status, string id)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        foreach (var forbidden in new[] { "stackTrace", "Exception", "connectionString", "SELECT ", "Npgsql", "SecureLab.Api" })
            Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
        var document = JsonDocument.Parse(json);
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("title").GetString()));
        output.WriteLine($"EVIDENCE {id} HTTP {(int)status} application/problem+json {json}");
        return document;
    }

    [Theory]
    [InlineData("missing-title", "title")]
    [InlineData("blank-title", "title")]
    [InlineData("long-title", "title")]
    [InlineData("title-length-before-trim", "title")]
    [InlineData("missing-description", "description")]
    [InlineData("blank-description", "description")]
    [InlineData("long-description", "description")]
    [InlineData("description-length-before-trim", "description")]
    [InlineData("missing-severity", "severity")]
    [InlineData("unknown-severity", "severity")]
    [InlineData("numeric-severity", "severity")]
    [InlineData("missing-date", "occurredAtUtc")]
    [InlineData("future-date", "occurredAtUtc")]
    public async Task T02_InvalidDto_ReturnsSafe400WithoutCreatingIncident(string scenario, string field)
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        var body = ValidBody(marker);
        switch (scenario)
        {
            case "missing-title": body.Remove("title"); break;
            case "blank-title": body["title"] = "   "; break;
            case "long-title": body["title"] = marker + new string('x', 161); break;
            case "title-length-before-trim": body["title"] = marker.PadRight(161); break;
            case "missing-description": body.Remove("description"); break;
            case "blank-description": body["description"] = "   "; break;
            case "long-description": body["description"] = new string('x', 4001); break;
            case "description-length-before-trim": body["description"] = "description".PadRight(4001); break;
            case "missing-severity": body.Remove("severity"); break;
            case "unknown-severity": body["severity"] = "Unknown"; break;
            case "numeric-severity": body["severity"] = "7"; break;
            case "missing-date": body.Remove("occurredAtUtc"); break;
            case "future-date": body["occurredAtUtc"] = "2099-01-01T00:00:00Z"; break;
            default: throw new ArgumentException("Unknown test scenario", nameof(scenario));
        }
        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            using var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest, $"T-02/{scenario}");
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            Assert.False(await db.Incidents.AnyAsync(item => item.Title.Contains(marker) || item.Description.Contains(marker)));
        }
        finally { await CleanupAsync(marker); }
    }

    [Fact]
    public async Task T01_ValidDto_IgnoresServerFieldsAndNormalizesUtc()
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        var suppliedId = Guid.NewGuid();
        var body = ValidBody(marker);
        body["title"] = $"  {marker}  ";
        body["description"] = $"  {marker} description  ";
        body["id"] = suppliedId;
        body["ownerUserId"] = DbSeeder.BobId;
        body["status"] = "Closed";
        body["createdAtUtc"] = "2000-01-01T00:00:00Z";
        body["updatedAtUtc"] = "2000-01-01T00:00:00Z";
        try
        {
            using var client = factory.CreateClient();
            var before = DateTimeOffset.UtcNow;
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            Assert.Equal(new[] { "createdAtUtc", "description", "id", "occurredAtUtc", "severity", "status", "title" },
                document.RootElement.EnumerateObject().Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());
            var created = JsonSerializer.Deserialize<CreatedIncidentResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.NotEqual(suppliedId, created.Id);
            Assert.Equal(marker, created.Title);
            Assert.Equal($"{marker} description", created.Description);
            Assert.Equal("New", created.Status);
            Assert.Equal("Low", created.Severity);
            Assert.Equal(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero), created.OccurredAtUtc);
            Assert.Equal(TimeSpan.Zero, created.OccurredAtUtc.Offset);
            Assert.InRange(created.CreatedAtUtc, before, DateTimeOffset.UtcNow);
            Assert.Equal($"/api/incidents/{created.Id}", response.Headers.Location?.OriginalString);
            var details = await client.GetFromJsonAsync<IncidentDetailsResponse>(response.Headers.Location);
            Assert.NotNull(details);
            Assert.Equal("Аліса Коваль", details.OwnerDisplayName);
            Assert.Equal("New", details.Status);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            var saved = await db.Incidents.AsNoTracking().SingleAsync(item => item.Id == created.Id);
            Assert.Equal(DbSeeder.AliceId, saved.OwnerUserId);
            Assert.Equal(saved.CreatedAtUtc, saved.UpdatedAtUtc);
            output.WriteLine($"EVIDENCE T-01/CP-01 HTTP 201 application/json {json}; Location={response.Headers.Location}; GET ownerDisplayName={details.OwnerDisplayName}; server fields verified in DB");
        }
        finally { await CleanupAsync(marker); }
    }

    [Theory]
    [InlineData("New")]
    [InlineData("Triaged")]
    [InlineData("InProgress")]
    [InlineData("Resolved")]
    public async Task T03_DuplicateTrimmedTitle_InNonClosedStatus_ReturnsSafe409(string status)
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            using var first = await client.PostAsJsonAsync("/api/incidents", ValidBody(marker));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
            await db.Incidents.Where(item => item.Title == marker)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, Enum.Parse<IncidentStatus>(status)));
            var duplicate = ValidBody(marker);
            duplicate["title"] = $"  {marker}  ";
            using var response = await client.PostAsJsonAsync("/api/incidents", duplicate);
            using var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict, $"T-03/{status}");
            Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("detail").GetString()));
            Assert.Equal(1, await db.Incidents.CountAsync(item => item.Title == marker));
        }
        finally { await CleanupAsync(marker); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task T03_ClosedOrDifferentCase_AllowsCreation(bool closed)
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            using var first = await client.PostAsJsonAsync("/api/incidents", ValidBody(marker));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            if (closed)
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
                await db.Incidents.Where(item => item.Title == marker)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, IncidentStatus.Closed));
            }
            var body = ValidBody(marker);
            // Keep the unique marker in Description so cleanup also finds the different-case title.
            if (!closed) body["title"] = marker.ToUpperInvariant();
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            output.WriteLine($"EVIDENCE T-03/{(closed ? "Closed" : "different-case")} HTTP 201 {await response.Content.ReadAsStringAsync()}");
        }
        finally { await CleanupAsync(marker); }
    }

    [Theory]
    [InlineData("High", 39, false)]
    [InlineData("High", 40, true)]
    [InlineData("Critical", 39, false)]
    [InlineData("Critical", 40, true)]
    public async Task T09_DescriptionAfterTrim_Uses40CharacterBoundary(string severity, int length, bool valid)
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        var body = ValidBody(marker);
        body["severity"] = severity;
        body["description"] = $"  {new string('x', length)}  ";
        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            if (valid)
            {
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var created = await response.Content.ReadFromJsonAsync<CreatedIncidentResponse>();
                Assert.NotNull(created);
                Assert.Equal(length, created.Description.Length);
                output.WriteLine($"EVIDENCE T-09/{severity}/{length} HTTP 201 {await response.Content.ReadAsStringAsync()}");
            }
            else
            {
                using var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest, $"T-09/{severity}/{length}");
                Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("description", out _));
            }
        }
        finally { await CleanupAsync(marker); }
    }

    [Fact]
    public async Task T01_MaximumLengthsAndNearFutureDate_AreAccepted()
    {
        var marker = $"Lr02-{Guid.NewGuid():N}";
        var body = ValidBody(marker);
        body["title"] = marker.PadRight(160, 'x');
        body["description"] = new string('x', 4000);
        body["occurredAtUtc"] = DateTimeOffset.UtcNow.AddMinutes(4);
        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync("/api/incidents", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CreatedIncidentResponse>();
            Assert.NotNull(created);
            Assert.Equal(160, created.Title.Length);
            Assert.Equal(4000, created.Description.Length);
            output.WriteLine("EVIDENCE T-01/limits HTTP 201; title.Length=160; description.Length=4000; date=UTC now+4min");
        }
        finally { await CleanupAsync(marker); }
    }

    [Fact]
    public async Task T06_UnknownIncident_ReturnsSafe404()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync($"/api/incidents/{Guid.NewGuid()}");
        using var problem = await ReadProblemAsync(response, HttpStatusCode.NotFound, "T-06");
        Assert.Equal("Інцидент не знайдено", problem.RootElement.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("detail").GetString()));
    }
}
