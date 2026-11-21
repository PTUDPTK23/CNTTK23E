using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Users;

namespace MediQueue.Web.Models.ViewModels.Users;

public sealed record UsersIndexViewModel(UserListQuery Query, PagedUsersResponse Results);

public sealed record UserManagementErrorViewModel(
    string Title,
    string Description,
    int StatusCode,
    bool CanRetry);

public sealed class CreateStaffViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(150, MinimumLength = 2, ErrorMessage = "Họ tên phải có từ 2 đến 150 ký tự.")]
    [Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(256)]
    [Display(Name = "Email")] public string Email { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(32)]
    [Display(Name = "Số điện thoại")] public string? PhoneNumber { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [Display(Name = "Vai trò")] public string Role { get; set; } = "Doctor";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu phải có ít nhất 12 ký tự.")]
    [DataType(DataType.Password), Display(Name = "Mật khẩu tạm thời")] public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu."), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận chưa khớp.")]
    [DataType(DataType.Password), Display(Name = "Xác nhận mật khẩu")] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class EditStaffViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(150, MinimumLength = 2)]
    [Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(32)]
    [Display(Name = "Số điện thoại")] public string? PhoneNumber { get; set; }
}
