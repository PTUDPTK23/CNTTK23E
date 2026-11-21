using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Authentication;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Password { get; init; } = string.Empty;
}
