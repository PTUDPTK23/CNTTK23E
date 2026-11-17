using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine("===============================================================");
Console.WriteLine("          EDUSUBMIT CLIENT - CHUONG TRINH NOP BAI TU DONG");
Console.WriteLine($"          Ket noi toi may chu: {apiBaseUrl}");
Console.WriteLine("===============================================================\n");

// =========================================================================
// NAC 1: Lay danh sach bai tap dang mo
// =========================================================================
Console.WriteLine(">>> NAC 1: Lay danh sach bai tap dang mo tu may chu...");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
Console.WriteLine("Danh sach bai tap dang co:");
foreach (var a in assignments ?? [])
{
    string tinhTrang = a.IsOpen ? "DANG MO" : "DA DONG";
    Console.WriteLine($" - [{a.Id}] {a.Title} | Trang thai: {tinhTrang}");
}

// =========================================================================
// NAC 2: Sinh vien nop bai cho bai tap 1
// =========================================================================
Console.WriteLine("\n>>> NAC 2: Gui yeu cau nop bai (POST /api/assignments/1/submissions)...");
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;

// Ban co the thay doi ma sinh vien va link repo cua ban tai day
string studentCode = "SV001";
string repoUrl = "https://github.com/sinhvien/Bai2-RESTful-API";

var createRequest = new CreateSubmissionRequest(studentCode, repoUrl);
var createResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", createRequest);

SubmissionDto? created = null;
if (createResponse.StatusCode == HttpStatusCode.Created)
{
    created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"[THANH CONG - 201 Created]");
    Console.WriteLine($"Da tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Doc lai luot nop: {created?.StudentCode} - {created?.RepositoryUrl}");
    Console.WriteLine($"Header Location nhan duoc: {createResponse.Headers.Location}");
}
else
{
    await PrintApiErrorAsync(createResponse);
    return;
}

// =========================================================================
// NAC 3: Doc lai chi tiet luot nop vua tao
// =========================================================================
Console.WriteLine("\n>>> NAC 3: Doc lai chi tiet luot nop bang Id vua nhan...");
if (created != null)
{
    var detailResponse = await http.GetAsync($"/api/submissions/{created.Id}");
    if (detailResponse.IsSuccessStatusCode)
    {
        var detail = await detailResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"[THANH CONG - 200 OK]");
        Console.WriteLine($"Xac nhan luot nop tren server:");
        Console.WriteLine($" - Id: {detail?.Id}");
        Console.WriteLine($" - Bai tap: {detail?.AssignmentId}");
        Console.WriteLine($" - Sinh vien: {detail?.StudentCode}");
        Console.WriteLine($" - Repository: {detail?.RepositoryUrl}");
        Console.WriteLine($" - Thoi gian nhan: {detail?.ReceivedAt}");
    }
    else
    {
        await PrintApiErrorAsync(detailResponse);
    }
}

// =========================================================================
// NAC 4: Thu gui du lieu sai de quan sat loi 400 Bad Request
// =========================================================================
Console.WriteLine("\n>>> NAC 4: Thu gui du lieu sai (thieu ma SV va kho ma nguon)...");
var invalidResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));
Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

// =========================================================================
// NAC MO RONG: Thu nop lai de quan sat loi 409 Conflict (trung lap luot nop)
// =========================================================================
Console.WriteLine("\n>>> NAC MO RONG: Thu nop trung lap bai tap da nop...");
var duplicateResponse = await http.PostAsJsonAsync($"/api/assignments/{selectedAssignmentId}/submissions", createRequest);
Console.WriteLine($"Ket qua nop trung: {(int)duplicateResponse.StatusCode} {duplicateResponse.StatusCode}");
await PrintApiErrorAsync(duplicateResponse);

Console.WriteLine("\n===============================================================");
Console.WriteLine("    HOAN THANH QUA TRINH CLIENT KIEM THU TU DONG DU 4 NAC!");
Console.WriteLine("===============================================================");

static async Task PrintApiErrorAsync(HttpResponseMessage r)
{
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Khai bao DTO ban sao o Client
record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);

