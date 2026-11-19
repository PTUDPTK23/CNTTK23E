using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediQueue.Appointment.Api.Security;

public sealed class JwtTokenService(
    IOptions<JwtOptions> optionsAccessor,
    TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions options = optionsAccessor.Value;
    private readonly byte[] signingKey = optionsAccessor.Value.GetSigningKeyBytes();

    public IssuedAccessToken IssueToken(
        Guid userId,
        string role,
        string securityStamp)
    {
        DateTimeOffset issuedAt = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = issuedAt.AddMinutes(options.AccessTokenLifetimeMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(issuedAt.UtcDateTime).ToString(),
                ClaimValueTypes.Integer64),
            new(JwtOptions.RoleClaim, role),
            new(
                JwtOptions.AuthenticationVersionClaim,
                ComputeAuthenticationVersion(securityStamp))
        ];

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(signingKey),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        SecurityToken token = handler.CreateToken(descriptor);
        return new IssuedAccessToken(handler.WriteToken(token), expiresAt);
    }

    public bool MatchesAuthenticationVersion(
        string securityStamp,
        string authenticationVersion)
    {
        byte[] expected = Encoding.UTF8.GetBytes(ComputeAuthenticationVersion(securityStamp));
        byte[] actual = Encoding.UTF8.GetBytes(authenticationVersion);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string ComputeAuthenticationVersion(string securityStamp)
    {
        using var hmac = new HMACSHA256(signingKey);
        byte[] digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(securityStamp));
        return Base64UrlEncoder.Encode(digest);
    }
}
