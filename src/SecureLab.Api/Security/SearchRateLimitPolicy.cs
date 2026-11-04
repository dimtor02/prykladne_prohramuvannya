using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace SecureLab.Api.Security;

public sealed class SearchRateLimitOptions
{
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
}

// Each authenticated principal has a quota; anonymous callers share their transport IP.
// Never trust X-Forwarded-For or an arbitrary client-supplied identity header here.
public sealed class SearchRateLimitPolicy(IOptions<SearchRateLimitOptions> options) : IRateLimiterPolicy<string>
{
    public const string Name = "incident-search";

    public RateLimitPartition<string> GetPartition(HttpContext context)
    {
        var subject = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        var key = subject is not null ? "user:" + subject
            : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.Value.PermitLimit,
            Window = TimeSpan.FromSeconds(options.Value.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => async (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(delay.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
            title: "Search rate limit exceeded.", type: "urn:securelab:search-rate-limit")
            .ExecuteAsync(context.HttpContext);
    };
}
