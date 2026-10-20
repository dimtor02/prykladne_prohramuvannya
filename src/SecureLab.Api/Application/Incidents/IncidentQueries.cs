using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SecureLab.Api.Data;
using SecureLab.Api.Data.Entities;
using SecureLab.Api.Presentation.Contracts;

namespace SecureLab.Api.Application.Incidents;

public sealed class IncidentQueries(SecureLabDbContext dbContext, ILogger<IncidentQueries> logger,
    IncidentAccessPolicy access)
{
    public async Task<IReadOnlyList<IncidentSearchResponse>> SearchAsync(
        string? q,
        string sortBy,
        CancellationToken cancellationToken)
    {
        // Escape LIKE metacharacters separately from SQL parameterization: q is a literal substring.
        var escaped = (q ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        var pattern = $"%{escaped}%";
        var query = dbContext.Incidents.AsNoTracking().Where(incident =>
            EF.Functions.ILike(incident.Title, pattern, "\\")
            || EF.Functions.ILike(incident.Description, pattern, "\\"));

        var ordered = sortBy switch
        {
            "createdAtUtc" => query.OrderByDescending(incident => incident.CreatedAtUtc),
            "severity" => query.OrderBy(incident => incident.Severity == IncidentSeverity.Critical ? 0
                : incident.Severity == IncidentSeverity.High ? 1
                : incident.Severity == IncidentSeverity.Medium ? 2 : 3),
            "status" => query.OrderBy(incident => incident.Status == IncidentStatus.New ? 0
                : incident.Status == IncidentStatus.Triaged ? 1
                : incident.Status == IncidentStatus.InProgress ? 2
                : incident.Status == IncidentStatus.Resolved ? 3 : 4),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy))
        };

        return await ordered.ThenBy(incident => incident.Id).Take(50)
            .Select(incident => new IncidentSearchResponse(
                incident.Id, incident.Title, incident.Description, incident.Severity.ToString(),
                incident.Status.ToString(), incident.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentSeveritySummaryResponse>> GetSeveritySummaryAsync(
        IncidentStatus? status,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Incidents.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(incident => incident.Status == status);
        }

        // PostgreSQL counts groups; missing enum levels are filled with zeros afterwards.
        var groups = await query
            .GroupBy(incident => incident.Severity)
            .Select(group => new { Severity = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        IncidentSeverity[] severityOrder =
            [IncidentSeverity.Low, IncidentSeverity.Medium, IncidentSeverity.High, IncidentSeverity.Critical];
        var counts = groups.ToDictionary(group => group.Severity, group => group.Count);
        var result = severityOrder
            .Select(severity => new IncidentSeveritySummaryResponse(
                severity.ToString(), counts.GetValueOrDefault(severity)))
            .ToList();

        logger.LogInformation("Loaded severity summary with status {Status}: {GroupCount} groups",
            status, result.Count);
        return result;
    }

    public async Task<IReadOnlyList<IncidentListItemResponse>> GetListAsync(
        IncidentStatus? status,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading incidents with status filter {Status}", status);

        var query = access.ScopeRead(dbContext.Incidents.AsNoTracking(), user);
        if (status is not null)
        {
            query = query.Where(incident => incident.Status == status);
        }

        return await query
            .OrderByDescending(incident => incident.CreatedAtUtc)
            .Select(incident => new IncidentListItemResponse(
                incident.Id,
                incident.Title,
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.OccurredAtUtc,
                incident.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<IncidentDetailsResponse?> GetDetailsAsync(Guid id, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading incident {IncidentId}", id);

        var includeInternal = IncidentAccessPolicy.IsAnalyst(user);
        return access.ScopeRead(dbContext.Incidents.AsNoTracking(), user)
            .Where(incident => incident.Id == id)
            .Select(incident => new IncidentDetailsResponse(
                incident.Id,
                incident.Title,
                incident.Description,
                incident.Severity.ToString(),
                incident.Status.ToString(),
                incident.OccurredAtUtc,
                incident.CreatedAtUtc,
                incident.Owner.DisplayName,
                incident.Comments
                    .Where(comment => !comment.IsInternal || includeInternal)
                    .OrderBy(comment => comment.CreatedAtUtc)
                    .Select(comment => new IncidentCommentResponse(
                        comment.Id,
                        comment.Author.DisplayName,
                        comment.Text,
                        comment.CreatedAtUtc))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
