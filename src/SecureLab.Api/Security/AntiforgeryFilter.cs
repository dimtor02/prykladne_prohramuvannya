using Microsoft.AspNetCore.Antiforgery;

namespace SecureLab.Api.Security;

// Apply only to cookie-authenticated mutations, after authentication/authorization.
public sealed class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public const string HeaderName = "X-CSRF-TOKEN";
    public const string TokenRoute = "/api/auth/csrf-token";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                type: "urn:securelab:csrf", title: "CSRF token is missing or invalid.");
        }
        return await next(context);
    }
}
