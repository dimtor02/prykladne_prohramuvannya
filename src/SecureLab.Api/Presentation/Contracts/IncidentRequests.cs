namespace SecureLab.Api.Presentation.Contracts;

public sealed record CreateIncidentRequest(
    string? Title,
    string? Description,
    string? Severity,
    DateTimeOffset? OccurredAtUtc);

public sealed record UpdateIncidentRequest(string? Title, string? Description);
