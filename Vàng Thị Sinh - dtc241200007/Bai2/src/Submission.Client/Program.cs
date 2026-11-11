using System.Net;
using System.Net.Http.Json;
var apiBaseUrl = args.Length > 0 ? args[0]
: "http://localhost:5000";
using var http = new HttpClient {
BaseAddress = new Uri(apiBaseUrl) };
//nac 1
var assignments = await http
.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
foreach (var a in assignments ?? [])
Console.WriteLine($"- {a.Id}: {a.Title}");

// tiếp ngay sau vòng foreach ở nấc 1
var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;
var createResponse = await http.PostAsJsonAsync(
$"/api/assignments/{selectedAssignmentId}/submissions",
new CreateSubmissionRequest("SV001", "https://example.com/..."));

if (createResponse.StatusCode != HttpStatusCode.Created) {
await PrintApiErrorAsync(createResponse);
return;
}

var created = await createResponse.Content
.ReadFromJsonAsync<SubmissionDto>();

Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
Console.WriteLine($"Trang thai ban dau: {created?.Status}");
Console.WriteLine($"Doc lai luot nop: SV001 - {createResponse.Headers.Location}");

static async Task PrintApiErrorAsync(HttpResponseMessage r) {
if (r.IsSuccessStatusCode) return;
var error = await r.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"{error?.Code}: {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt);
record ApiError(string Code, string Message);