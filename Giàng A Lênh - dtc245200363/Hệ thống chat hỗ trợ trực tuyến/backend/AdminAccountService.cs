using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ChatSupport.Backend;

public sealed class AdminAccountService
{
    private const int Iterations = 600_000;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<AdminUser?> AuthenticateAsync(ChatSupportDbContext db, string username, string password)
    {
        var account = await db.AdminUsers.SingleOrDefaultAsync(user => user.Username.ToLower() == username.Trim().ToLower());
        if (account is null) return null;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(account.PasswordSalt);
            expectedHash = Convert.FromBase64String(account.PasswordHash);
        }
        catch (FormatException)
        {
            return null;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash) ? account : null;
    }

    public static async Task SeedInitialAdminAsync(ChatSupportDbContext db, string contentRoot)
    {
        if (await db.AdminUsers.AnyAsync()) return;

        var path = Path.Combine(contentRoot, "App_Data", "admin-account.json");
        if (!File.Exists(path)) return;

        var seed = JsonSerializer.Deserialize<AdminSeed>(await File.ReadAllTextAsync(path), JsonOptions);
        if (seed is null || string.IsNullOrWhiteSpace(seed.Username) || string.IsNullOrWhiteSpace(seed.PasswordHash)) return;

        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            DisplayName = seed.DisplayName,
            Username = seed.Username,
            Role = seed.Role,
            PasswordSalt = seed.PasswordSalt,
            PasswordHash = seed.PasswordHash
        });
        await db.SaveChangesAsync();
    }

    private sealed record AdminSeed(string DisplayName, string Username, string Role, string PasswordSalt, string PasswordHash);
}
