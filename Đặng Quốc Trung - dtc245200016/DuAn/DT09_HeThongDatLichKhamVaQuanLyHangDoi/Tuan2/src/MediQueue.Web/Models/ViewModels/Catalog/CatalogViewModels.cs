using System.ComponentModel.DataAnnotations;
using MediQueue.Contracts.Catalogs;

namespace MediQueue.Web.Models.ViewModels.Catalog;

public sealed record DepartmentsIndexViewModel(DepartmentListQuery Query, PagedDepartmentsResponse Results);
public sealed record RoomsIndexViewModel(RoomListQuery Query, PagedRoomsResponse Results);
public sealed record CatalogErrorViewModel(string Title, string Description, int StatusCode, string ReturnController);

public sealed class DepartmentFormViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mã chuyên khoa."), StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ, số, gạch nối hoặc gạch dưới.")]
    [Display(Name = "Mã chuyên khoa")] public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập tên chuyên khoa."), StringLength(200)]
    [Display(Name = "Tên chuyên khoa")] public string Name { get; set; } = string.Empty;
}

public sealed class RoomFormViewModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mã phòng."), StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Mã chỉ gồm chữ, số, gạch nối hoặc gạch dưới.")]
    [Display(Name = "Mã phòng")] public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập tên phòng."), StringLength(200)]
    [Display(Name = "Tên phòng")] public string Name { get; set; } = string.Empty;
    [StringLength(300), Display(Name = "Vị trí")] public string? Location { get; set; }
}
