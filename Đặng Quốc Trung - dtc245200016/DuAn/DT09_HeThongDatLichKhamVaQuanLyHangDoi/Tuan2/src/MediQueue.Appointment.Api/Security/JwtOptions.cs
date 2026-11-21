using Microsoft.IdentityModel.Tokens;

namespace MediQueue.Appointment.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string AuthenticationVersionClaim = "auth_ver";
    public const string RoleClaim = "role";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; init; } = 30;

    public int ClockSkewSeconds { get; init; } = 30;

    public string SigningKey { get; init; } = string.Empty;

    public string Algorithm { get; init; } = SecurityAlgorithms.HmacSha256;

    public byte[] GetSigningKeyBytes() => Convert.FromBase64String(SigningKey);
}
