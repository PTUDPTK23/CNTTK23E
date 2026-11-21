using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediQueue.Appointment.Api.Security;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (options.Issuer != "MediQueue.Appointment.Api")
        {
            failures.Add("Jwt:Issuer must be 'MediQueue.Appointment.Api'.");
        }

        if (options.Audience != "MediQueue.Web")
        {
            failures.Add("Jwt:Audience must be 'MediQueue.Web'.");
        }

        if (options.AccessTokenLifetimeMinutes != 30)
        {
            failures.Add("Jwt:AccessTokenLifetimeMinutes must be 30.");
        }

        if (options.ClockSkewSeconds is < 0 or > 30)
        {
            failures.Add("Jwt:ClockSkewSeconds must be between 0 and 30.");
        }

        if (options.Algorithm != SecurityAlgorithms.HmacSha256)
        {
            failures.Add("Jwt:Algorithm must be HS256.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Jwt:SigningKey is required through User Secrets or an environment variable.");
        }
        else
        {
            try
            {
                if (options.GetSigningKeyBytes().Length < 32)
                {
                    failures.Add("Jwt:SigningKey must contain at least 256 bits of random data.");
                }
            }
            catch (FormatException)
            {
                failures.Add("Jwt:SigningKey must be a valid Base64 value.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    public static void ValidateAndThrow(JwtOptions options)
    {
        ValidateOptionsResult result = new JwtOptionsValidator().Validate(null, options);
        if (result.Failed)
        {
            throw new OptionsValidationException(
                JwtOptions.SectionName,
                typeof(JwtOptions),
                result.Failures);
        }
    }
}
