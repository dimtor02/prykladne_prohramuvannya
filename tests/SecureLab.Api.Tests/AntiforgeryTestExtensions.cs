using System.Net.Http.Json;
using SecureLab.Api.Security;

namespace SecureLab.Api.Tests;

public static class AntiforgeryTestExtensions
{
    private sealed record TokenResponse(string Token);

    // Explicit opt-in for tests that need to reach validation/authorization/mutation logic.
    // CookieClient.LoginAsync itself deliberately does not add a token.
    public static async Task UseAntiforgeryTokenAsync(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(AntiforgeryFilter.HeaderName);
        using var response = await client.GetAsync(AntiforgeryFilter.TokenRoute);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (string.IsNullOrWhiteSpace(body?.Token)) throw new InvalidOperationException("Missing CSRF token.");
        client.DefaultRequestHeaders.Add(AntiforgeryFilter.HeaderName, body.Token);
    }
}
