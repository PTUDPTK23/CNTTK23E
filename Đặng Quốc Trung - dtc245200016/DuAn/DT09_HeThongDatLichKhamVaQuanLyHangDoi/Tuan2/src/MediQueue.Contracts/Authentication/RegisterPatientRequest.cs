using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Authentication;

public sealed class RegisterPatientRequest
{
    [Required]
    [StringLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; init; }

    public DateOnly? DateOfBirth { get; init; }
}
