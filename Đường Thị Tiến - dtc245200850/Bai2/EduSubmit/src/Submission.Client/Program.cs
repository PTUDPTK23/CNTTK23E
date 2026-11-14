using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== KET NOI TOI EDUSUBMIT API TAI: {apiBaseUrl} ===\n");

Console.WriteLine("--- NAC 1: DANH SACH BAI TAP DANG MO ---");
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? []) {
    Console.WriteLine($"- {a.Id}: {a.Title}");
}

Console.WriteLine("\n--- NAC 2: NOP BAI TAP (POST) ---");
var selectedAssignmentId = assignments?.FirstOrDefault(a => a.IsOpen)?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("SV001", "https://github.com/dtc245200850-spec/EduSubmit-Bai2"));

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
Console.WriteLine($"Da tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai tai: {createResponse.Headers.Location}");

Console.WriteLine("\n--- NAC 3: DOC LAI LUOT NOP (GET /api/submissions/{id}) ---");
if (created != null) {
    var getResponse = await http.GetAsync($"/api/submissions/{created.Id}");
    if (getResponse.IsSuccessStatusCode) {
        var subDetail = await getResponse.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Doc lai luot nop: {subDetail?.StudentCode} - {subDetail?.RepositoryUrl}");
        Console.WriteLine($"Ma luot nop (GUID): {subDetail?.Id}");
    } else {
        await PrintApiErrorAsync(getResponse);
    }
}

Console.WriteLine("\n--- NAC 4: CO Y GUI DU LIEU SAI DE QUAN SAT LOI 400 ---");
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

Console.WriteLine($"Thu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
await PrintApiErrorAsync(invalidResponse);

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
    if (r.IsSuccessStatusCode) return;
    var error = await r.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);
