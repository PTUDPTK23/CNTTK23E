using MediQueue.Web.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace MediQueue.IntegrationTests.Web;

public sealed class AuthenticationSessionStoreTests
{
    [Fact]
    public async Task Store_isolates_session_by_random_handle_and_user()
    {
        var time = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var store = new InMemoryAuthenticationSessionStore(cache, time);
        Guid owner = Guid.NewGuid();
        var session = new AuthenticationSession("unguessable-handle", owner, "server-token", time.GetUtcNow().AddDays(1));

        await store.StoreAsync(session);

        Assert.Equal(session, await store.GetAsync("unguessable-handle", owner));
        Assert.Null(await store.GetAsync("unguessable-handle", Guid.NewGuid()));
        Assert.Null(await store.GetAsync("another-handle", owner));
    }

    [Fact]
    public async Task Store_expires_and_deletes_session()
    {
        var time = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var store = new InMemoryAuthenticationSessionStore(cache, time);
        Guid owner = Guid.NewGuid();
        await store.StoreAsync(new AuthenticationSession("session", owner, "token", time.GetUtcNow().AddDays(1)));

        time.Advance(TimeSpan.FromDays(2));
        Assert.Null(await store.GetAsync("session", owner));

        await store.StoreAsync(new AuthenticationSession("second", owner, "token", time.GetUtcNow().AddDays(1)));
        await store.RemoveAsync("second");
        Assert.Null(await store.GetAsync("second", owner));
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset current) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => current;
        public void Advance(TimeSpan duration) => current = current.Add(duration);
    }
}
