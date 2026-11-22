using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

Console.WriteLine($"=== KET NOI TOI DOCUMENT API: {apiBaseUrl} ===\n");

// 1. Lấy danh sách tài liệu hiện có
var docs = await http.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
Console.WriteLine("1. Danh sach tai lieu ban dau:");
foreach (var d in docs ?? [])
{
    Console.WriteLine($"   - [{d.Id}] {d.Title} | Trang thai: {d.Status} | Nguoi xu ly: {d.AssignedTo}");
}

// 2. Tạo tài liệu mới (Draft)
Console.WriteLine("\n2. Tao tai lieu moi...");
var createReq = new CreateDocumentRequest(
    Title: "Ke hoach bao ve do an",
    Content: "Noi dung trinh bay kien truc he thong phan tan.",
    CreatedBy: "SV003",
    AssignedTo: "GV02");

var createRes = await http.PostAsJsonAsync("/api/documents", createReq);
if (createRes.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createRes);
    return;
}

var createdDoc = await createRes.Content.ReadFromJsonAsync<DocumentDto>();
Console.WriteLine($"   -> Thanh cong (201 Created): [{createdDoc?.Id}] {createdDoc?.Title} ({createdDoc?.Status})");
Console.WriteLine($"   -> Header Location: {createRes.Headers.Location}");

var docId = createdDoc!.Id;

// 3. Gửi tài liệu vào quy trình phê duyệt (Draft -> Submitted)
Console.WriteLine($"\n3. Gui tai lieu #{docId} vao quy trinh phe duyet...");
var submitRes = await http.PostAsJsonAsync(
    $"/api/documents/{docId}/submit",
    new SubmitDocumentRequest("SV003", "Em xin gui thay xem xet a."));

if (!submitRes.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(submitRes);
    return;
}
var submittedDoc = await submitRes.Content.ReadFromJsonAsync<DocumentDto>();
Console.WriteLine($"   -> Trang thai moi: {submittedDoc?.Status}");

// 4. Phê duyệt tài liệu (Submitted -> Approved)
Console.WriteLine($"\n4. Phe duyet tai lieu #{docId}...");
var approveRes = await http.PostAsJsonAsync(
    $"/api/documents/{docId}/approve",
    new ReviewDocumentRequest("GV02", "Dong y ke hoach, cho phep trien khai."));

if (!approveRes.IsSuccessStatusCode)
{
    await PrintApiErrorAsync(approveRes);
    return;
}

// 5. Đọc lại chi tiết tài liệu & in toàn bộ lịch sử các bước xử lý
var detail = await http.GetFromJsonAsync<DocumentDto>($"/api/documents/{docId}");
Console.WriteLine($"\n5. Chi tiet & Lich su xu ly tai lieu #{detail?.Id} (Trang thai: {detail?.Status}):");
foreach (var h in detail?.History ?? [])
{
    Console.WriteLine($"   * [{h.Timestamp:HH:mm:ss}] {h.Action} boi {h.Actor}: {h.Note}");
}

// 6. Thử nghiệm xử lý lỗi: Cố tình từ chối tài liệu ĐÃ ĐƯỢC PHÊ DUYỆT (kỳ vọng 409 Conflict)
Console.WriteLine($"\n6. Thu nghiem loi nghiep vu (tu choi tai lieu #{docId} da Approved):");
var invalidRejectRes = await http.PostAsJsonAsync(
    $"/api/documents/{docId}/reject",
    new ReviewDocumentRequest("GV02", "Thu tu choi lai"));

Console.WriteLine($"   -> Ma trang thai nhan duoc: {(int)invalidRejectRes.StatusCode} {invalidRejectRes.StatusCode}");
await PrintApiErrorAsync(invalidRejectRes);

// Hàm dùng chung để đọc mọi lỗi chuẩn ApiError từ server
static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"   -> Loi API [{error?.Code}]: {error?.Message}");
}

// Khai báo bản sao DTO hợp đồng phía Client
record CreateDocumentRequest(string Title, string Content, string CreatedBy, string? AssignedTo);
record SubmitDocumentRequest(string? Actor, string? Note);
record ReviewDocumentRequest(string Reviewer, string? Note);
record ApprovalHistoryDto(string Action, string Actor, string Note, DateTimeOffset Timestamp);
record DocumentDto(
    int Id,
    string Title,
    string Content,
    string CreatedBy,
    string AssignedTo,
    string Status,
    DateTimeOffset CreatedAt,
    List<ApprovalHistoryDto> History);
record ApiError(string Code, string Message);