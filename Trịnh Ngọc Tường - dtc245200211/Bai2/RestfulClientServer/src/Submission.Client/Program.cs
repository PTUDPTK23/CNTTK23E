using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Nac 1: lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var openAssignments = (assignments ?? []).Where(a => a.IsOpen).ToList();

Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var a in openAssignments) {
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

// Nac 2: nop bai cho bai tap dang mo dau tien, kiem tra ma 201 truoc khi doc body
var selectedAssignmentId = openAssignments.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://example.com/sv001/rest-api"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    Console.WriteLine($"\nKhong the nop bai: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");

// Nac 3: doc lai luot nop vua tao theo header Location
var locationUri = createResponse.Headers.Location?.ToString() ?? $"/api/submissions/{created?.Id}";
var fetched = await http.GetFromJsonAsync<SubmissionDto>(locationUri);
Console.WriteLine($"Doc lai luot nop: {fetched?.StudentCode} - {fetched?.RepositoryUrl}");

// Nac 4: co y gui yeu cau loi de kiem tra xu ly ApiError 400
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
