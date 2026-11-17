// EduSubmit – Submission.Client (Ứng dụng Console máy khách)
// Học phần: Phát triển ứng dụng phân tán – Bài 2: RESTful API
// Chạy mặc định: dotnet run --project src/Submission.Client -- http://localhost:5000

using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

try
{
    // ── BƯỚC 1: LẤY DANH SÁCH BÀI TẬP ──────────────────────────────────────
    var getAssignmentsResponse = await http.GetAsync("/api/assignments");
    if (!getAssignmentsResponse.IsSuccessStatusCode)
    {
        await PrintApiErrorAsync(getAssignmentsResponse);
        return;
    }

    var assignments = await getAssignmentsResponse.Content.ReadFromJsonAsync<List<AssignmentDto>>();
    Console.WriteLine("Danh sach bai tap dang mo:");
    foreach (var a in assignments ?? [])
    {
        var trangThai = a.IsOpen ? "Dang mo" : "Da dong";
        Console.WriteLine($" - {a.Id}: {a.Title} ({trangThai})");
    }

    // ── BƯỚC 2: CHỌN MỘT BÀI TẬP ĐANG MỞ ───────────────────────────────────
    var openAssignment = assignments?.FirstOrDefault(a => a.IsOpen);
    if (openAssignment is null)
    {
        Console.WriteLine("\nKhong tim thay bai tap nao dang mo de nop bai.");
        return;
    }

    Console.WriteLine($"\nChon bai tap {openAssignment.Id} ('{openAssignment.Title}') de nop.");

    // ── BƯỚC 3 & 4: NỘP BÀI TẬP BẰNG POST VÀ KIỂM TRA MÃ TRẠNG THÁI ─────────
    var request = new CreateSubmissionRequest(
        StudentCode: "SV001",
        RepositoryUrl: "https://example.com/sv001/rest-api"
    );

    var createResponse = await http.PostAsJsonAsync($"/api/assignments/{openAssignment.Id}/submissions", request);

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        Console.WriteLine($"\nNop bai that bai (Ma: {(int)createResponse.StatusCode}):");
        await PrintApiErrorAsync(createResponse);
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

    // ── BƯỚC 5: ĐỌC LẠI CHI TIẾT LƯỢT NỘP BẰNG GUID ─────────────────────────
    if (created is not null)
    {
        var getSubmissionResponse = await http.GetAsync($"/api/submissions/{created.Id}");
        if (getSubmissionResponse.IsSuccessStatusCode)
        {
            var detail = await getSubmissionResponse.Content.ReadFromJsonAsync<SubmissionDto>();
            Console.WriteLine($"Doc lai luot nop thanh cong: SV={detail?.StudentCode} - Repo={detail?.RepositoryUrl} - Luc={detail?.ReceivedAt:HH:mm:ss dd/MM/yyyy}");
        }
        else
        {
            await PrintApiErrorAsync(getSubmissionResponse);
        }
    }

    // ── BƯỚC THỬ NGHIỆM PHÁ LỖI: GỬI DỮ LIỆU SAI ĐỂ QUAN SÁT LỖI 400 ────────
    Console.WriteLine("\nThu gui yeu cau loi (thieu ma sinh vien):");
    var invalidRequest = new CreateSubmissionRequest("", "");
    var invalidResponse = await http.PostAsJsonAsync($"/api/assignments/{openAssignment.Id}/submissions", invalidRequest);
    Console.WriteLine($"Phan hoi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    await PrintApiErrorAsync(invalidResponse);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\nLoi ket noi: Khong the ket noi den Server tai {apiBaseUrl}. Vui long kiem tra API dang chay hay chua.");
    Console.WriteLine($"Chi tiet loi: {ex.Message}");
}

// ── HÀM DÙNG CHUNG ĐỌC APIERROR KHÔNG SẬP ──────────────────────────────────
static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;

    try
    {
        var error = await r.Content.ReadFromJsonAsync<ApiError>();
        if (error is not null && !string.IsNullOrWhiteSpace(error.Code))
        {
            Console.WriteLine($"{error.Code}: {error.Message}");
            return;
        }
    }
    catch
    {
        // Body không phải dạng JSON ApiError (ví dụ text/plain hoặc rỗng)
    }

    var rawText = await r.Content.ReadAsStringAsync();
    if (!string.IsNullOrWhiteSpace(rawText))
    {
        Console.WriteLine($"Chi tiet: {rawText}");
    }
}

// ── DTO RIÊNG CỦA MÁY KHÁCH (ĐỘC LẬP MÃ NGUỒN VỚI SERVER) ───────────────────
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
