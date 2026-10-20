using SecureLab.Api.Application.Incidents;

namespace SecureLab.Api.Presentation.Endpoints;

public static class IncidentAccessResults
{
    // Identical response for unknown and inaccessible ids, including child ids.
    public static IResult Denied(IncidentAccess decision, HttpContext context) => decision switch
    {
        IncidentAccess.Forbidden => Problem(403, "Forbidden", context),
        IncidentAccess.InvalidTransition => Problem(409, "Дозволено лише перехід New → Triaged", context),
        _ => Problem(404, "Інцидент не знайдено", context)
    };

    private static IResult Problem(int status, string title, HttpContext context) =>
        Results.Problem(statusCode: status, title: title,
            extensions: new Dictionary<string, object?> { ["traceId"] = IncidentAccessPolicy.TraceId(context) });
}
