using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Authentication;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(256)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [StringLength(256, MinimumLength = 12)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
