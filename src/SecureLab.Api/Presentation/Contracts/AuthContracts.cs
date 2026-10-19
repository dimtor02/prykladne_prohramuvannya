namespace SecureLab.Api.Presentation.Contracts;

// Server-managed identity fields and roles are deliberately absent.
public sealed record RegisterRequest(string? UserName, string? Email, string? DisplayName, string? Password);
public sealed record LoginRequest(string? UserName, string? Password);
public sealed record UserProfileResponse(Guid Id, string UserName, string DisplayName);
