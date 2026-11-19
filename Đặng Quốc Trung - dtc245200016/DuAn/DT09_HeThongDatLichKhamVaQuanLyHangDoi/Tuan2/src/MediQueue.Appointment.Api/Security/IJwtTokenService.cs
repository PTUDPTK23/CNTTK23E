namespace MediQueue.Appointment.Api.Security;

public interface IJwtTokenService
{
    IssuedAccessToken IssueToken(
        Guid userId,
        string role,
        string securityStamp);

    bool MatchesAuthenticationVersion(
        string securityStamp,
        string authenticationVersion);
}

public sealed record IssuedAccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);
