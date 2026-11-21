using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using MediQueue.Appointment.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediQueue.Appointment.Api.Security;

public static class MediQueueAuthenticationExtensions
{
    public const string LoginPolicy = "account-login";
    public const string RegisterPolicy = "account-register";

    public static IServiceCollection AddMediQueueAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtOptions options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? new JwtOptions();
        JwtOptionsValidator.ValidateAndThrow(options);

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        byte[] signingKey = options.GetSigningKeyBytes();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = JwtOptions.RoleClaim
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        IAuthenticationStateValidator validator =
                            context.HttpContext.RequestServices
                                .GetRequiredService<IAuthenticationStateValidator>();
                        if (context.Principal is null ||
                            !await validator.IsCurrentAsync(
                                context.Principal,
                                context.HttpContext.RequestAborted))
                        {
                            context.Fail("Phiên đăng nhập không còn hiệu lực.");
                        }
                    }
                };
            });

        services.AddAuthorization();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationStateValidator, AuthenticationStateValidator>();
        services.AddScoped<IAccountService, AccountService>();

        services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Quá nhiều yêu cầu",
                    Detail = "Vui lòng thử lại sau."
                }, cancellationToken);
            };
            rateLimiter.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            rateLimiter.AddPolicy(RegisterPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    private static string GetClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString();
}
