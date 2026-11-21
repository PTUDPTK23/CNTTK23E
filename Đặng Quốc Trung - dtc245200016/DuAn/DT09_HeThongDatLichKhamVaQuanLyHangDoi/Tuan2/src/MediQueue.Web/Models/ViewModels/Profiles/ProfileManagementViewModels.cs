using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Profiles;

namespace MediQueue.Web.Models.ViewModels.Profiles;

public sealed record DoctorsIndexViewModel(DoctorListQuery Query, PagedDoctorsResponse Results);
public sealed record PatientsIndexViewModel(PatientListQuery Query, PagedPatientsResponse Results);

public sealed class CreateDoctorProfileViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn tài khoản Bác sĩ."), Display(Name = "Tài khoản Bác sĩ")] public Guid UserId { get; set; }
    [Required(ErrorMessage = "Vui lòng chọn chuyên khoa."), Display(Name = "Chuyên khoa")] public Guid DepartmentId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị."), StringLength(200, MinimumLength = 2), Display(Name = "Tên bác sĩ")] public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyList<AvailableDoctorAccountResponse> Accounts { get; set; } = [];
    public IReadOnlyList<DoctorDepartmentOptionResponse> Departments { get; set; } = [];
}

public sealed class EditDoctorProfileViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng chọn chuyên khoa."), Display(Name = "Chuyên khoa")] public Guid DepartmentId { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị."), StringLength(200, MinimumLength = 2), Display(Name = "Tên bác sĩ")] public string DisplayName { get; set; } = string.Empty;
    [Required] public string RowVersion { get; set; } = string.Empty;
    public IReadOnlyList<DoctorDepartmentOptionResponse> Departments { get; set; } = [];
}

public sealed class EditPatientProfileViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(200, MinimumLength = 2), Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(32), Display(Name = "Số điện thoại")] public string? PhoneNumber { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày sinh")] public DateOnly? DateOfBirth { get; set; }
    [Required] public string RowVersion { get; set; } = string.Empty;
}
