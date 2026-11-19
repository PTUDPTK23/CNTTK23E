using System.IdentityModel.Tokens.Jwt;
using MediQueue.Appointment.Api.Security;
using MediQueue.Domain.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediQueue.IntegrationTests.Authentication;

public sealed class JwtSecurityTests
{
    private static readonly byte[] Key = Enumerable.Range(1, 32)
        .Select(number => (byte)number)
        .ToArray();

    [Fact]
    public void Options_validator_accepts_official_configuration()
    {
        ValidateOptionsResult result = new JwtOptionsValidator().Validate(null, CreateOptions());

        Assert.Same(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData("bad issuer", "MediQueue.Web", 30, 0, "HS256")]
    [InlineData("MediQueue.Appointment.Api", "bad audience", 30, 0, "HS256")]
    [InlineData("MediQueue.Appointment.Api", "MediQueue.Web", 31, 0, "HS256")]
    [InlineData("MediQueue.Appointment.Api", "MediQueue.Web", 30, 31, "HS256")]
    [InlineData("MediQueue.Appointment.Api", "MediQueue.Web", 30, 0, "HS384")]
    public void Options_validator_rejects_non_official_configuration(
        string issuer,
        string audience,
        int lifetime,
        int skew,
        string algorithm)
    {
        JwtOptions options = CreateOptionsWithValues(
            issuer, audience, lifetime, skew, algorithm);

        Assert.True(new JwtOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public async Task Issued_token_has_required_claims_and_validates_with_official_parameters()
    {
        JwtOptions options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options), TimeProvider.System);

        IssuedAccessToken issued = service.IssueToken(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            SystemRoles.Patient,
            "test-security-stamp");
        var handler = new JwtSecurityTokenHandler();
        TokenValidationResult validation = await handler.ValidateTokenAsync(
            issued.Value,
            Parameters(options));
        Assert.True(validation.IsValid);
        JwtSecurityToken token = handler.ReadJwtToken(issued.Value);

        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtOptions.RoleClaim && claim.Value == SystemRoles.Patient);
        Assert.Contains(token.Claims, claim => claim.Type == JwtOptions.AuthenticationVersionClaim);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
        Assert.InRange(issued.ExpiresAtUtc - DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(30).Add(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Token_validation_rejects_tampering_wrong_key_issuer_and_audience()
    {
        JwtOptions options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options), TimeProvider.System);
        string token = service.IssueToken(Guid.NewGuid(), SystemRoles.Doctor, "stamp").Value;
        var handler = new JwtSecurityTokenHandler();

        string tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        Assert.False((await handler.ValidateTokenAsync(tampered, Parameters(options))).IsValid);

        JwtOptions wrongKey = CreateOptions(Convert.ToBase64String(Key.Reverse().ToArray()));
        Assert.False((await handler.ValidateTokenAsync(token, Parameters(wrongKey))).IsValid);

        TokenValidationParameters wrongIssuer = Parameters(options);
        wrongIssuer.ValidIssuer = "wrong";
        Assert.False((await handler.ValidateTokenAsync(token, wrongIssuer)).IsValid);

        TokenValidationParameters wrongAudience = Parameters(options);
        wrongAudience.ValidAudience = "wrong";
        Assert.False((await handler.ValidateTokenAsync(token, wrongAudience)).IsValid);
    }

    [Fact]
    public async Task Token_validation_rejects_expired_token_and_wrong_algorithm()
    {
        JwtOptions options = CreateOptions();
        var handler = new JwtSecurityTokenHandler();
        DateTime now = DateTime.UtcNow;
        var expired = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            expires: now.AddMinutes(-1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(options.GetSigningKeyBytes()),
                SecurityAlgorithms.HmacSha256));
        var wrongAlgorithm = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            expires: now.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Enumerable.Range(1, 64)
                    .Select(number => (byte)number)
                    .ToArray()),
                SecurityAlgorithms.HmacSha384));

        Assert.False((await handler.ValidateTokenAsync(
            handler.WriteToken(expired), Parameters(options))).IsValid);
        Assert.False((await handler.ValidateTokenAsync(
            handler.WriteToken(wrongAlgorithm), Parameters(options))).IsValid);
    }

    [Fact]
    public void Authentication_version_is_bound_to_security_stamp()
    {
        JwtOptions options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options), TimeProvider.System);
        string token = service.IssueToken(Guid.NewGuid(), SystemRoles.Patient, "stamp-a").Value;
        string authenticationVersion = new JwtSecurityTokenHandler()
            .ReadJwtToken(token)
            .Claims
            .Single(claim => claim.Type == JwtOptions.AuthenticationVersionClaim)
            .Value;

        Assert.True(service.MatchesAuthenticationVersion("stamp-a", authenticationVersion));
        Assert.False(service.MatchesAuthenticationVersion("stamp-b", authenticationVersion));
    }

    private static JwtOptions CreateOptions(string? key = null) => new()
    {
        Issuer = "MediQueue.Appointment.Api",
        Audience = "MediQueue.Web",
        AccessTokenLifetimeMinutes = 30,
        ClockSkewSeconds = 0,
        Algorithm = SecurityAlgorithms.HmacSha256,
        SigningKey = key ?? Convert.ToBase64String(Key)
    };

    private static JwtOptions CreateOptionsWithValues(
        string issuer, string audience, int lifetime, int skew, string algorithm) => new()
        {
            Issuer = issuer,
            Audience = audience,
            AccessTokenLifetimeMinutes = lifetime,
            ClockSkewSeconds = skew,
            Algorithm = algorithm,
            SigningKey = Convert.ToBase64String(Key)
        };

    private static TokenValidationParameters Parameters(JwtOptions options) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(options.GetSigningKeyBytes()),
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero
    };
}
