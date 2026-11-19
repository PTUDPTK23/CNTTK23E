namespace MediQueue.Contracts.Authentication;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    Guid UserId,
    string? FullName,
    string Email,
    string Role);
