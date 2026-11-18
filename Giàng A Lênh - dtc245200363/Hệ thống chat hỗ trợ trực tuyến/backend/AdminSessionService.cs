using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ChatSupport.Backend;

public sealed class AdminSessionService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> sessions = new();

    public string Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        sessions[token] = DateTimeOffset.UtcNow.AddHours(8);
        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !sessions.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (expiresAt > DateTimeOffset.UtcNow)
        {
            return true;
        }

        sessions.TryRemove(token, out _);
        return false;
    }

    public void Revoke(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            sessions.TryRemove(token, out _);
        }
    }
}
