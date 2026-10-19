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

public sealed class IncidentSearchTests(SecureLabApiFactory factory, ITestOutputHelper output)
    : IClassFixture<SecureLabApiFactory>
{
    private async Task CleanupAsync(string marker)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
        await db.Incidents.Where(item => item.Title.StartsWith(marker)).ExecuteDeleteAsync();
    }

    private static Incident NewIncident(string title, string description, int index = 0) => new()
    {
        Id = Guid.NewGuid(),
        OwnerUserId = index % 2 == 0 ? DbSeeder.AliceId : DbSeeder.BobId,
        Title = title,
        Description = description,
        Severity = (IncidentSeverity)(index % 4),
        Status = (IncidentStatus)(index % 5),
        OccurredAtUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero),
        CreatedAtUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero).AddMinutes(index % 3),
        UpdatedAtUtc = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)
    };

    [Theory]
    [InlineData("USB", "20000000-0000-0000-0000-000000000005")]
    [InlineData("usb", "20000000-0000-0000-0000-000000000005")]
    [InlineData("O'Brien", "20000000-0000-0000-0000-000000000004")]
    [InlineData("комп'ютерного", "20000000-0000-0000-0000-000000000003")]
    public async Task T04_LegitimateSearch_ReturnsExactSeedMatch(string q, string expectedId)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/incidents/search?q=" + Uri.EscapeDataString(q));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var items = await response.Content.ReadFromJsonAsync<List<IncidentSearchResponse>>();
        Assert.NotNull(items);
        Assert.Equal(Guid.Parse(expectedId), Assert.Single(items).Id);
        output.WriteLine($"EVIDENCE T-04/{q} HTTP 200 {await response.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task S02_OriginalControlInput_ReturnsNoUnrelatedRecords()
    {
        using var client = factory.CreateClient();
        const string control = "zz-no-match' OR TRUE -- ";
        using var response = await client.GetAsync("/api/incidents/search?q=" + Uri.EscapeDataString(control));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<IncidentSearchResponse>>();
        Assert.NotNull(items);
        Assert.Empty(items);
        output.WriteLine("EVIDENCE S-02 HTTP 200 application/json []; exact empty result set, no Alice/Bob seed records");
    }

    [Theory]
    [InlineData("price")]
    [InlineData("Severity")]
    [InlineData(" ")]
    public async Task T05_UnknownSort_ReturnsSafe400(string sortBy)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/incidents/search?sortBy=" + Uri.EscapeDataString(sortBy));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("sortBy", out _));
        foreach (var forbidden in new[] { "stackTrace", "Exception", "connectionString", "SELECT ", "Npgsql", "SecureLab.Api" })
            Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
        output.WriteLine($"EVIDENCE T-05/{sortBy} HTTP 400 application/problem+json {json}");
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("\\")]
    [InlineData("\\%_")]
    public async Task Search_Metacharacters_AreLiteralInTitleAndDescription(string literal)
    {
        var marker = $"Lr02Search-{Guid.NewGuid():N}";
        var titleMatch = NewIncident(marker + literal, "Title-only match");
        var descriptionMatch = NewIncident(marker + "-description", marker + literal);
        var decoy = NewIncident(marker + "X", "A different substring");
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
                db.Incidents.AddRange(titleMatch, descriptionMatch, decoy);
                await db.SaveChangesAsync();
            }
            using var client = factory.CreateClient();
            var items = await client.GetFromJsonAsync<List<IncidentSearchResponse>>(
                "/api/incidents/search?q=" + Uri.EscapeDataString(marker + literal));
            Assert.NotNull(items);
            Assert.Equal(new[] { titleMatch.Id, descriptionMatch.Id }.OrderBy(id => id), items.Select(item => item.Id));
            output.WriteLine($"EVIDENCE SEARCH/literal-{JsonSerializer.Serialize(literal)} HTTP 200; exact two IDs matched Title/Description; decoy excluded");
        }
        finally { await CleanupAsync(marker); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("createdAtUtc")]
    [InlineData("severity")]
    [InlineData("status")]
    public async Task Search_SemanticOrderThenId_IsAppliedBeforeTake50(string? sortBy)
    {
        var marker = $"Lr02Search-{Guid.NewGuid():N}";
        // More than 50 rows and repeated primary keys expose both early limiting and missing tie-breaks.
        var rows = Enumerable.Range(0, 60).Select(index => NewIncident($"{marker}-{index}", "Sort test", index)).ToArray();
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SecureLabDbContext>();
                db.Incidents.AddRange(rows.Reverse());
                await db.SaveChangesAsync();
            }
            var sorted = sortBy switch
            {
                "severity" => rows.OrderByDescending(item => item.Severity),
                "status" => rows.OrderBy(item => item.Status),
                _ => rows.OrderByDescending(item => item.CreatedAtUtc)
            };
            var expectedIds = sorted.ThenBy(item => item.Id).Take(50).Select(item => item.Id).ToArray();
            using var client = factory.CreateClient();
            var url = "/api/incidents/search?q=" + Uri.EscapeDataString(marker)
                + (sortBy is null ? "" : "&sortBy=" + Uri.EscapeDataString(sortBy));
            var items = await client.GetFromJsonAsync<List<IncidentSearchResponse>>(url);
            Assert.NotNull(items);
            Assert.Equal(expectedIds, items.Select(item => item.Id).ToArray());
            output.WriteLine($"EVIDENCE SEARCH/sort-{sortBy ?? "missing"} HTTP 200; exact first 50 of 60 IDs; semantic rank/date order and Id tie-break verified");
        }
        finally { await CleanupAsync(marker); }
    }
}
