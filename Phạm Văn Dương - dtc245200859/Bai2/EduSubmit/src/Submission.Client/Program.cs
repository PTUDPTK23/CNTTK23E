using System.Net;
using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Buoc 1: lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");

Console.WriteLine("Danh sach bai tap dang mo:");
foreach (var assignment in assignments ?? [])
{
    Console.WriteLine($"[{assignment.Id}] {assignment.Title}: {assignment.Description}");
}

var selectedAssignmentId = assignments?.FirstOrDefault()?.Id ?? 1;

// Buoc 2: nop bai va kiem tra 201 Created + Location
var createRequest = new CreateSubmissionRequest(
    "DTC245200859",
    "https://github.com/phamzduong-pixel/CNTTK23E");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    createRequest);

if (createResponse.StatusCode == HttpStatusCode.Created)
{
    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"\nDa tao luot nop: {created?.Id}");
    Console.WriteLine($"Trang thai ban dau: {created?.Status}");
    Console.WriteLine($"Doc lai theo Location: {createResponse.Headers.Location}");

    // Buoc 3: doc lai luot nop vua tao
    if (createResponse.Headers.Location is Uri location)
    {
        var detail = await http.GetFromJsonAsync<SubmissionDto>(location);
        Console.WriteLine($"Doc lai luot nop: {detail?.StudentCode} - {detail?.RepositoryUrl}");
    }
}
else
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"\nLoi: {(int)createResponse.StatusCode} {createResponse.StatusCode}");
    Console.WriteLine($"{error?.Code}: {error?.Message}");
}

// Buoc 4: co y gui du lieu sai de doc loi 400
var invalidResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignmentId}/submissions",
    new CreateSubmissionRequest("", ""));

var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ApiError>();
Console.WriteLine($"\nThu gui yeu cau loi: {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
Console.WriteLine($"{invalidError?.Code}: {invalidError?.Message}");

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
