using Microsoft.EntityFrameworkCore;
using SecureLab.Api.Application.Incidents;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Presentation.Endpoints;

public static class IncidentMutationEndpoints
{
    public static void MapIncidentMutationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/incidents").RequireAuthorization().WithTags("Incidents");
        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateIncidentContent").Produces(204).ProducesValidationProblem()
            .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteIncident").Produces(204).ProducesProblem(403).ProducesProblem(404);
        group.MapPost("/{id:guid}/triage", TriageAsync)
            .WithName("TriageIncident").Produces(204)
            .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
        app.MapGet("/api/incident-comments/{id:guid}", GetCommentAsync)
            .RequireAuthorization().WithTags("Incident comments")
            .Produces<IncidentCommentResponse>().ProducesProblem(404);
    }

    private static async Task<IResult> UpdateAsync(Guid id, UpdateIncidentRequest request,
        HttpContext context, SecureLabDbContext db, IncidentAccessPolicy access, CancellationToken ct)
    {
        // Tracked entity; permission is decided before validation and before any mutation.
        var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
        var decision = access.Decide(context.User, incident, IncidentOperation.UpdateContent);
        if (decision != IncidentAccess.Allow) return IncidentAccessResults.Denied(decision, context);
        var title = request.Title?.Trim() ?? "";
        var description = request.Description?.Trim() ?? "";
        var errors = new Dictionary<string, string[]>();
        if (title.Length == 0 || request.Title!.Length > 160)
            errors["title"] = ["Заголовок обов'язковий; не більше 160 символів до Trim."];
        if (description.Length == 0 || request.Description!.Length > 4000)
            errors["description"] = ["Опис обов'язковий; не більше 4000 символів до Trim."];
        if (incident!.Severity is IncidentSeverity.High or IncidentSeverity.Critical && description.Length < 40)
            errors["description"] = ["Для High або Critical потрібно щонайменше 40 символів опису."];
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        // Preserve the inherited active-title conflict rule on content updates too.
        if (await db.Incidents.AnyAsync(x => x.Id != id && x.Title == title
            && x.Status != IncidentStatus.Closed, ct))
            return Results.Problem(statusCode: 409, title: "Конфлікт інциденту",
                detail: "Інцидент із таким заголовком уже існує та ще не закритий.");
        incident.Title = title;
        incident.Description = description;
        incident.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(Guid id, HttpContext context,
        SecureLabDbContext db, IncidentAccessPolicy access, CancellationToken ct)
    {
        var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
        var decision = access.Decide(context.User, incident, IncidentOperation.Delete);
        if (decision != IncidentAccess.Allow) return IncidentAccessResults.Denied(decision, context);
        db.Incidents.Remove(incident!);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> TriageAsync(Guid id, HttpContext context,
        SecureLabDbContext db, IncidentAccessPolicy access, CancellationToken ct)
    {
        var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
        var decision = access.Decide(context.User, incident, IncidentOperation.Triage);
        if (decision != IncidentAccess.Allow)
        {
            access.AuditDeniedTriage(context, id);
            return IncidentAccessResults.Denied(decision, context);
        }
        // Next state and actor come from server state and the verified cookie principal.
        var now = DateTimeOffset.UtcNow;
        incident!.Status = IncidentStatus.Triaged;
        incident.UpdatedAtUtc = now;
        db.IncidentStatusHistory.Add(new IncidentStatusHistory
        {
            Id = Guid.NewGuid(), IncidentId = incident.Id,
            ChangedByUserId = IncidentAccessPolicy.SubjectId(context.User)!.Value,
            OldStatus = IncidentStatus.New, NewStatus = IncidentStatus.Triaged,
            Note = "Первинну оцінку завершено аналітиком.", CreatedAtUtc = now
        });
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetCommentAsync(Guid id, HttpContext context,
        SecureLabDbContext db, IncidentAccessPolicy access, CancellationToken ct)
    {
        // Direct child id never bypasses authorization for the parent incident.
        var parents = access.ScopeRead(db.Incidents.AsNoTracking(), context.User);
        var includeInternal = IncidentAccessPolicy.IsAnalyst(context.User);
        var response = await db.IncidentComments.AsNoTracking()
            .Where(comment => comment.Id == id && (!comment.IsInternal || includeInternal)
                && parents.Any(incident => incident.Id == comment.IncidentId))
            .Select(comment => new IncidentCommentResponse(comment.Id, comment.Author.DisplayName,
                comment.Text, comment.CreatedAtUtc)).SingleOrDefaultAsync(ct);
        return response is null ? IncidentAccessResults.Denied(IncidentAccess.Hidden, context)
            : Results.Ok(response);
    }
}
