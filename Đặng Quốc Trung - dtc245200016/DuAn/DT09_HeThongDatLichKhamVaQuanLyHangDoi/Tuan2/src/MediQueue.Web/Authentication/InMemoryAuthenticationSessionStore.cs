using Microsoft.Extensions.Caching.Memory;

namespace MediQueue.Web.Authentication;

public sealed class InMemoryAuthenticationSessionStore(
    IMemoryCache cache,
    TimeProvider timeProvider) : IAuthenticationSessionStore
{
    private const string CacheKeyPrefix = "mvc-auth-session:";

    public Task<AuthenticationSession?> GetAsync(
        string sessionId,
        Guid expectedUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId) ||
            !cache.TryGetValue(CacheKeyPrefix + sessionId, out AuthenticationSession? session) ||
            session is null)
        {
            return Task.FromResult<AuthenticationSession?>(null);
        }

        if (session.UserId != expectedUserId || session.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            cache.Remove(CacheKeyPrefix + sessionId);
            return Task.FromResult<AuthenticationSession?>(null);
        }

        return Task.FromResult<AuthenticationSession?>(session);
    }

    public Task StoreAsync(AuthenticationSession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.Set(
            CacheKeyPrefix + session.SessionId,
            session,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = session.ExpiresAtUtc,
                Size = 1
            });
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            cache.Remove(CacheKeyPrefix + sessionId);
        }

        return Task.CompletedTask;
    }
}
