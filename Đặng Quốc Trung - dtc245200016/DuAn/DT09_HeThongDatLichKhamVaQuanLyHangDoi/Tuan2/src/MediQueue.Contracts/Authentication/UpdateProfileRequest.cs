using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Authentication;

public sealed class UpdateProfileRequest
{
    [StringLength(200)]
    public string? FullName { get; init; }

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; init; }
}
