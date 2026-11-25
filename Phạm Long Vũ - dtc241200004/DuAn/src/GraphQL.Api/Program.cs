var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var HocSinhs = new List<HocSinhDto>
{
    new("SV001", "DTC2351001", "Nguyen Van An", "CNTT-K22A", "an0123na@ictu.edu.vn"),
    new("SV002", "DTC2351002", "Tran Thi Binh", "CNTT-K22A", "binhk22hn@ictu.edu.vn"),
    new("SV003", "DTC2351003", "Le Hoang Nam", "KTPM-K22B", "namk22ht@ictu.edu.vn"),
    new("SV004", "DTC2351004", "Phan Thien Lam", "KTPM-K23A", "lamk23bg@ictu.edu.vn"),
};
var HocPhans = new List<HocPhanDto>
{
    new("HP001", "Lap trinh Web", "CNTT-K22A", 3, "2026-2"),
    new("HP002", "CSDL Nâng Cao", "CNTT-K22A", 4, "2026-1"),
    new("HP003", "Khoa hoc du lieu", "KTPM-K22B", 3, "2026-2"),
    new("HP004", "Tri tue nhan tao", "KTPM-K23A", 3, "2026-1")
};

var DKHocphans = new List<DKHocPhanDto>
{
    new(1, "SV001", "HP002", "2026-1", "Đạt", 8.5),
    new(2, "SV001", "HP001", "2026-2", "Đang học", null),
    new(3, "SV002", "HP001", "2026-2", "Đang học", null),
    new(4, "SV004", "HP004", "2026-1", "Trượt", 0.0)
};
app.MapGet("/", () => Results.Ok(new
{
    service = "GraphQL.Api",
    status = "running"
}));

app.MapGet("/api/students", () => Results.Ok(HocSinhs));

app.MapGet("/api/courses", (string? hocky) => 
{
    IEnumerable<HocPhanDto> query = HocPhans;
    if (!string.IsNullOrEmpty(hocky))
    {
        query = query.Where(c => c.HocKy.Equals(hocky.Trim(), StringComparison.OrdinalIgnoreCase));
    }
    return Results.Ok(query.ToList());
});

app.MapGet("/api/HocSinhs/{id}", (string id) =>
{
    var hocSinh = HocSinhs.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    if (hocSinh == null)
    {
        return Results.NotFound(new ApiError("STUDENT_NOT_FOUND", $"Khong tim thay sinh vien co ma '{id}'."));
    }
    var dsDangKyCuaSV = DKHocphans
        .Where(dk => dk.MaSinhVien.Equals(hocSinh.Id, StringComparison.OrdinalIgnoreCase))
        .Select(dk =>
        {
            var hocPhan = HocPhans.FirstOrDefault(hp =>
                hp.Id.Equals(dk.MaHocPhan, StringComparison.OrdinalIgnoreCase));
            return new ChiTietDKHocPhanDto(
                dk.Id,
                dk.HocKy,
                dk.TrangThai,
                dk.Diem,
                hocPhan);
        })
        .ToList();

    var chiTietHocSinh = new ChiTietHocSinhDto(
        hocSinh.Id,
        hocSinh.MaSinhVien,
        hocSinh.Hoten,
        hocSinh.lophoc,
        hocSinh.Email,
        TongTinChi: dsDangKyCuaSV.Sum(dk => dk.HocPhan?.SoTinChi ?? 0),
        DanhSachDangKy: dsDangKyCuaSV);
    return Results.Ok(chiTietHocSinh);
});

// Lọc danh sách đăng ký học phần theo học kỳ, mã học phần hoặc trạng thái
app.MapGet("/api/DKHocPhans", (string? hocky, string? mahocphan, string? trangthai) =>
{
    IEnumerable<DKHocPhanDto> query = DKHocphans;

    if (!string.IsNullOrWhiteSpace(hocky))
        query = query.Where(dk => dk.HocKy.Equals(hocky.Trim(), StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(mahocphan))
        query = query.Where(dk => dk.MaHocPhan.Equals(mahocphan.Trim(), StringComparison.OrdinalIgnoreCase));

    if (!string.IsNullOrWhiteSpace(trangthai))
        query = query.Where(dk => dk.TrangThai.Equals(trangthai.Trim(), StringComparison.OrdinalIgnoreCase));

    return Results.Ok(query.ToList());
});

// Đăng ký học phần mới (Kiểm tra : Rỗng -> Có SV không -> Có Môn không -> Có trùng không)
app.MapPost("/api/DKHocPhans", (TaoDKHocPhanRequest request) =>
{
    // Kiểm tra rỗng -> 400 Bad Request
    if (string.IsNullOrWhiteSpace(request.MaSinhVien) ||
        string.IsNullOrWhiteSpace(request.MaHocPhan) ||
        string.IsNullOrWhiteSpace(request.HocKy))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_ENROLLMENT",
            "Ma sinh vien, ma hoc phan va hoc ky khong duoc de trong."));
    }

    var maSV = request.MaSinhVien.Trim().ToUpperInvariant();
    var maHP = request.MaHocPhan.Trim().ToUpperInvariant();
    var hocKy = request.HocKy.Trim();

    // Kiểm tra sinh viên tồn tại -> 404 Not Found
    if (!HocSinhs.Any(s => s.Id.Equals(maSV, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.NotFound(new ApiError(
            "STUDENT_NOT_FOUND",
            $"Khong tim thay sinh vien '{maSV}'."));
    }

    // Kiểm tra học phần tồn tại -> 404 Not Found
    if (!HocPhans.Any(hp => hp.Id.Equals(maHP, StringComparison.OrdinalIgnoreCase)))
    {
        return Results.NotFound(new ApiError(
            "COURSE_NOT_FOUND",
            $"Khong tim thay hoc phan '{maHP}'."));
    }

    // Kiểm tra đăng ký trùng -> 409 Conflict
    bool biTrung = DKHocphans.Any(dk =>
        dk.MaSinhVien.Equals(maSV, StringComparison.OrdinalIgnoreCase) &&
        dk.MaHocPhan.Equals(maHP, StringComparison.OrdinalIgnoreCase) &&
        dk.HocKy.Equals(hocKy, StringComparison.OrdinalIgnoreCase));

    if (biTrung)
    {
        return Results.Conflict(new ApiError(
            "DUPLICATE_ENROLLMENT",
            $"Sinh vien '{maSV}' da dang ky hoc phan '{maHP}' trong hoc ky '{hocKy}' roi."));
    }

    // Tạo mới lượt đăng ký -> 201 Created
    var idMoi = DKHocphans.Count == 0 ? 1 : DKHocphans.Max(dk => dk.Id) + 1;
    var dkMoi = new DKHocPhanDto(idMoi, maSV, maHP, hocKy, "Đang học", null);
    DKHocphans.Add(dkMoi);

    return Results.Created($"/api/DKHocPhans/{dkMoi.Id}", dkMoi);
});

app.Run();

record HocSinhDto(string Id, string MaSinhVien, string Hoten, string lophoc, string Email);
record HocPhanDto(string Id, string TenHocPhan, string Lophoc, int SoTinChi, string HocKy);
record DKHocPhanDto(int Id, string MaSinhVien, string MaHocPhan, string HocKy, string TrangThai, double? Diem);
record ApiError(string Code, string Message);
record ChiTietDKHocPhanDto(int Id, string HocKy, string TrangThai, double? Diem, HocPhanDto? HocPhan);
record ChiTietHocSinhDto(string Id, string MaSinhVien, string Hoten, string lophoc, string Email, int TongTinChi, List<ChiTietDKHocPhanDto> DanhSachDangKy);
record TaoDKHocPhanRequest(string MaSinhVien, string MaHocPhan, string HocKy);