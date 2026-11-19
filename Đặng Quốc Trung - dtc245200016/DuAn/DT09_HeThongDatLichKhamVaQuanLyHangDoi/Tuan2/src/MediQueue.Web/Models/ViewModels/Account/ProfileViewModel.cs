using System.ComponentModel.DataAnnotations;

namespace MediQueue.Web.Models.ViewModels.Account;

public sealed class ProfileViewModel
{
    public Guid UserId { get; set; }

    [StringLength(200, ErrorMessage = "Họ và tên không vượt quá 200 ký tự.")]
    [Display(Name = "Họ và tên")]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại chưa đúng định dạng.")]
    [StringLength(32)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Vai trò")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Ngày sinh")]
    public DateOnly? DateOfBirth { get; set; }

    public bool CanEditFullName => Role is "Patient" or "Doctor";
}
