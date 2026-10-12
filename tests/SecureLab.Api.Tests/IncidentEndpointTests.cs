using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SecureLab.Api.Data;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Tests;

public sealed class IncidentEndpointTests(SecureLabApiFactory factory)
    : IClassFixture<SecureLabApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("", "Low,Medium,High,Critical", "1,1,1,0")]
    [InlineData("?status=Triaged", "Low,Medium,High,Critical", "0,1,0,0")]
    [InlineData("?status=Resolved", "Low,Medium,High,Critical", "0,0,0,0")]
    public async Task GetSeveritySummary_ReturnsOnlyOrderedAggregateFields(
        string query, string expectedSeverities, string expectedCounts)
    {
        using var response = await _client.GetAsync("/api/incidents/severity-summary" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        var items = document.RootElement.EnumerateArray().ToArray();
        Assert.Equal(expectedSeverities, string.Join(",", items.Select(item => item.GetProperty("severity").GetString())));
        Assert.Equal(expectedCounts, string.Join(",", items.Select(item => item.GetProperty("count").GetInt32())));
        foreach (var item in items)
        {
            Assert.Equal(new[] { "count", "severity" },
                item.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
        }
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("1")]
    [InlineData("")]
    public async Task GetSeveritySummary_ForUnsupportedStatus_ReturnsValidationProblem(string status)
    {
        using var response = await _client.GetAsync("/api/incidents/severity-summary?status=" + status);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("status", out _));
    }

    [Fact]
    public async Task GetList_ReturnsSeededIncidents()
    {
        var incidents = await _client.GetFromJsonAsync<List<IncidentListItemResponse>>(
            "/api/incidents");

        Assert.NotNull(incidents);
        Assert.Contains(incidents, incident => incident.Id == DbSeeder.AliceIncidentId);
        Assert.Contains(incidents, incident => incident.Id == DbSeeder.BobIncidentId);
    }

    [Fact]
    public async Task GetDetails_ForUnknownId_ReturnsProblemDetails404()
    {
        using var response = await _client.GetAsync($"/api/incidents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDetails_DoesNotExposeInternalOwnerFields()
    {
        using var response = await _client.GetAsync(
            $"/api/incidents/{DbSeeder.AliceIncidentId}");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(document.RootElement.TryGetProperty("ownerUserId", out _));
        Assert.False(document.RootElement.TryGetProperty("email", out _));
        Assert.Equal("Аліса Коваль", document.RootElement.GetProperty("ownerDisplayName").GetString());
    }

    [Fact]
    public async Task ClientScript_DoesNotUseDangerousInnerHtmlSink()
    {
        var script = await _client.GetStringAsync("/app.js");

        Assert.DoesNotContain("innerHTML", script, StringComparison.Ordinal);
        Assert.Contains("textContent", script, StringComparison.Ordinal);
    }
}
