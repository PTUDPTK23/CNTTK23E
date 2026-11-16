using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};


// Lấy danh sách sinh viên
Console.WriteLine("DANH SACH SINH VIEN");
var dsHocSinh = await http.GetFromJsonAsync<List<HocSinhDto>>("/api/students");
foreach (var sv in dsHocSinh ?? [])
{
    Console.WriteLine($"  [{sv.Id}] {sv.MaSinhVien} - {sv.Hoten} (Lop: {sv.lophoc})");
}

// Lọc danh sách học phần mở trong học kỳ 2026-2
Console.WriteLine("\n HOC PHAN HOC KY 2026-2 ");
var dsHocPhan = await http.GetFromJsonAsync<List<HocPhanDto>>("/api/courses?hocky=2026-2");
foreach (var hp in dsHocPhan ?? [])
{
    Console.WriteLine($"  [{hp.Id}] {hp.TenHocPhan} - {hp.SoTinChi} tin chi (Lop: {hp.Lophoc})");
}

// Tra cứu chi tiết sinh viên SV001 kèm các môn học
Console.WriteLine("\nCHI TIET SINH VIEN SV001");
var chiTietRes = await http.GetAsync("/api/HocSinhs/SV001");
if (chiTietRes.StatusCode == HttpStatusCode.OK)
{
    var sv = await chiTietRes.Content.ReadFromJsonAsync<ChiTietHocSinhDto>();
    Console.WriteLine($"  Sinh vien: {sv?.Hoten} | Lop: {sv?.lophoc} | Tong tin chi: {sv?.TongTinChi}");
    foreach (var dk in sv?.DanhSachDangKy ?? [])
    {
        Console.WriteLine($"    -> [{dk.HocKy}] {dk.HocPhan?.TenHocPhan} ({dk.HocPhan?.SoTinChi} TC) | Trang thai: {dk.TrangThai} | Diem: {dk.Diem?.ToString() ?? "Chua co"}");
    }
}

// Đăng ký học phần mới cho SV003 -> Kiểm tra mã 201 Created trước khi đọc body
Console.WriteLine("\nDANG KY MON HP001 CHO SV003");
var yeuCauDK = new TaoDKHocPhanRequest("SV003", "HP001", "2026-2");
var dkResponse = await http.PostAsJsonAsync("/api/DKHocPhans", yeuCauDK);

if (dkResponse.StatusCode == HttpStatusCode.Created)
{
    var dkMoi = await dkResponse.Content.ReadFromJsonAsync<DKHocPhanDto>();
    Console.WriteLine($"  Da tao luot dang ky moi: #{dkMoi?.Id} | Trang thai: {dkMoi?.TrangThai}");
    Console.WriteLine($"  Doc lai tai (Location): {dkResponse.Headers.Location}");
}
else
{
    await InLoiApiAsync(dkResponse);
}

// Cố ý gửi trùng (409) và gửi sai mã học phần (404) để kiểm tra đọc ApiError
Console.WriteLine("\n THU GUI YEU CAU LOI (409 & 404) ");

var trungResponse = await http.PostAsJsonAsync("/api/DKHocPhans", yeuCauDK);
Console.Write($"  - Gui trung lan 2 -> {(int)trungResponse.StatusCode} {trungResponse.StatusCode}: ");
await InLoiApiAsync(trungResponse);

var saiMonResponse = await http.PostAsJsonAsync("/api/DKHocPhans",
    new TaoDKHocPhanRequest("SV003", "HP999", "2026-2"));
Console.Write($"  - Gui sai ma mon  -> {(int)saiMonResponse.StatusCode} {saiMonResponse.StatusCode}: ");
await InLoiApiAsync(saiMonResponse);

static async Task InLoiApiAsync(HttpResponseMessage response)
{
    if (response.IsSuccessStatusCode) return;
    var loi = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{loi?.Code}: {loi?.Message}");
}

record HocSinhDto(string Id, string MaSinhVien, string Hoten, string lophoc, string Email);
record HocPhanDto(string Id, string TenHocPhan, string Lophoc, int SoTinChi, string HocKy);
record DKHocPhanDto(int Id, string MaSinhVien, string MaHocPhan, string HocKy, string TrangThai, double? Diem);
record ApiError(string Code, string Message);
record ChiTietDKHocPhanDto(int Id, string HocKy, string TrangThai, double? Diem, HocPhanDto? HocPhan);
record ChiTietHocSinhDto(string Id, string MaSinhVien, string Hoten, string lophoc, string Email, int TongTinChi, List<ChiTietDKHocPhanDto> DanhSachDangKy);
record TaoDKHocPhanRequest(string MaSinhVien, string MaHocPhan, string HocKy);