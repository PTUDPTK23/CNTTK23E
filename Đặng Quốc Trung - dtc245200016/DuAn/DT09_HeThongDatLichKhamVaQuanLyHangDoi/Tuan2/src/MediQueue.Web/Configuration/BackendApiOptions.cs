using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace MediQueue.Web.Configuration;

public sealed class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    [Required]
    public Uri BaseAddress { get; init; } = new("https://localhost:7101/");

    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 15;
}

public sealed class BackendApiOptionsValidator : IValidateOptions<BackendApiOptions>
{
    public ValidateOptionsResult Validate(string? name, BackendApiOptions options)
    {
        if (!options.BaseAddress.IsAbsoluteUri)
        {
            return ValidateOptionsResult.Fail("BackendApi:BaseAddress phải là địa chỉ tuyệt đối.");
        }

        if (!string.Equals(options.BaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail("BackendApi:BaseAddress bắt buộc sử dụng HTTPS.");
        }

        return ValidateOptionsResult.Success;
    }
}
