using Microsoft.EntityFrameworkCore;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Presentation.Endpoints;

// Controlled LOCAL lab experiment. This commit must never be deployed.
public static class IncidentMutationEndpoints
{
    public static void MapIncidentMutationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/incidents").RequireAuthorization();
        group.MapPut("/{id:guid}", async (Guid id, UpdateIncidentRequest request,
            SecureLabDbContext db, CancellationToken ct) =>
        {
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (incident is null) return Results.NotFound();
            incident.Title = request.Title ?? incident.Title;
            incident.Description = request.Description ?? incident.Description;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        group.MapPost("/{id:guid}/triage", async (Guid id, SecureLabDbContext db, CancellationToken ct) =>
        {
            var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (incident is null) return Results.NotFound();
            incident.Status = IncidentStatus.Triaged;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }
}
