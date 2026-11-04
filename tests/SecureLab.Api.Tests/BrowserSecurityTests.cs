using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureLab.Api.Data;

namespace SecureLab.Api.Tests;

public sealed class BrowserSecurityTests(SecureLabApiFactory factory)
    : IClassFixture<SecureLabApiFactory>
{
    [Theory]
    [InlineData("http://localhost:4173", true)]
    [InlineData("http://localhost:4174", false)]
    public async Task SimpleHealth_ExecutesForBothOrigins_ButOnlyAllowsTrustedOrigin(
        string origin, bool allowed)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }
        else Assert.DoesNotContain(response.Headers, h => h.Key.StartsWith("Access-Control-"));
    }

    [Theory]
    [InlineData("http://localhost:4173", true)]
    [InlineData("http://localhost:4174", false)]
    public async Task Preflight_AllowsExactOriginMethodAndHeader(string origin, bool allowed)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "x-lab-cors-probe");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        if (allowed)
        {
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("GET", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
            Assert.Equal("X-Lab-Cors-Probe", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
        }
        else Assert.DoesNotContain(response.Headers, h => h.Key.StartsWith("Access-Control-"));
    }

    [Theory]
    [InlineData("http://localhost:4173", true)]
    [InlineData("http://localhost:4174", false)]
    public async Task AnonymousProtectedGet_Remains401_WithCorsOnlyForTrustedOrigin(
        string origin, bool allowed)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/incidents");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/auth-lab.html")]
    [InlineData("/browser-lab.html")]
    [InlineData("/lr04-probe.html")]
    public async Task ClientDocuments_EnforceCspAndBrowserHeaders(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var policy = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("script-src 'self'", policy);
        Assert.Contains("frame-ancestors 'self'", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
        Assert.DoesNotContain("*", policy);
        Assert.False(response.Headers.Contains("Content-Security-Policy-Report-Only"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Production_DoesNotRegisterOrApplyTeachingCorsPolicy()
    {
        var connection = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("SecureLab");
        // Program reads the connection before Build(), so provide it at host creation.
        // Suite-level parallelization is disabled; always restore the process environment.
        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__SecureLab");
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SecureLab", connection);
            using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
            using var client = production.CreateClient();
            var provider = production.Services.GetService<ICorsPolicyProvider>();
            if (provider is not null)
                Assert.Null(await provider.GetPolicyAsync(new Microsoft.AspNetCore.Http.DefaultHttpContext(), "Lab05BrowserProbe"));
            using var request = new HttpRequestMessage(HttpMethod.Get, "/browser-lab.html");
            request.Headers.Add("Origin", "http://localhost:4173");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(response.Headers, h => h.Key.StartsWith("Access-Control-"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__SecureLab", previous);
        }
    }
}
