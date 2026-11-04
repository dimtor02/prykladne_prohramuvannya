using System.Security.Claims;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;

namespace SecureLab.Api.Application.Incidents;

public enum IncidentOperation { Read, UpdateContent, Delete, Triage }
public enum IncidentAccess { Allow, Hidden, Forbidden, InvalidTransition }

// One decision for role + ownership + resource context. Roles come from the cookie principal.
public sealed class IncidentAccessPolicy(ILogger<IncidentAccessPolicy> logger)
{
    public static string TraceId(HttpContext context) =>
        System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;

    public static Guid? SubjectId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static bool IsAnalyst(ClaimsPrincipal user) =>
        SubjectId(user) is not null && user.IsInRole(DbSeeder.AnalystRole);

    public IQueryable<Incident> ScopeRead(IQueryable<Incident> query, ClaimsPrincipal user)
    {
        var subject = SubjectId(user);
        var analyst = IsAnalyst(user);
        var reporter = subject is not null && user.IsInRole(DbSeeder.ReporterRole);
        return query.Where(incident => analyst || (reporter && incident.OwnerUserId == subject));
    }

    public IncidentAccess Decide(ClaimsPrincipal user, Incident? incident, IncidentOperation operation)
    {
        var subject = SubjectId(user);
        var analyst = IsAnalyst(user);
        var owner = subject is not null && user.IsInRole(DbSeeder.ReporterRole)
            && incident?.OwnerUserId == subject;
        if (incident is null || (!owner && !analyst)) return IncidentAccess.Hidden;

        return operation switch
        {
            IncidentOperation.Read => IncidentAccess.Allow,
            IncidentOperation.UpdateContent or IncidentOperation.Delete =>
                owner ? IncidentAccess.Allow : IncidentAccess.Forbidden,
            IncidentOperation.Triage when !analyst => IncidentAccess.Forbidden,
            IncidentOperation.Triage when incident.Status != IncidentStatus.New => IncidentAccess.InvalidTransition,
            IncidentOperation.Triage => IncidentAccess.Allow,
            _ => IncidentAccess.Forbidden
        };
    }

    public void AuditDeniedTriage(HttpContext context, Guid resourceId)
    {
        logger.LogWarning(
            "{EventName} subject={SubjectId} action={Action} resource={ResourceId} result={Result} occurredAtUtc={OccurredAtUtc:O} traceId={TraceId}",
            "authorization.denied", SubjectId(context.User), "incident.triage", resourceId,
            "denied", DateTimeOffset.UtcNow, TraceId(context));
    }
}
