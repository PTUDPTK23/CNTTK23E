namespace MediQueue.Web.Authentication;

public sealed record AuthenticationSession(
    string SessionId,
    Guid UserId,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);

public interface IAuthenticationSessionStore
{
    Task<AuthenticationSession?> GetAsync(
        string sessionId,
        Guid expectedUserId,
        CancellationToken cancellationToken = default);

    Task StoreAsync(AuthenticationSession session, CancellationToken cancellationToken = default);

    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}
