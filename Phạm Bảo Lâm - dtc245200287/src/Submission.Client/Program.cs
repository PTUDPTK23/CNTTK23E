// EduSubmit - Submission.Client: client HTTP doc lap voi API.

using System.Net.Http.Json;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
if (!apiBaseUrl.EndsWith('/'))
{
    apiBaseUrl += "/";
}

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

var assignments = await http.GetFromJsonAsync<List<Assignment>>("api/assignments")
    ?? new List<Assignment>();
Console.WriteLine("Danh sach bai tap:");
foreach (var assignment in assignments)
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "SV001",
    RepositoryUrl: "https://example.com/sv001/bai-tap-1");

using var response = await http.PostAsJsonAsync("api/submissions", request);
response.EnsureSuccessStatusCode();

var submitted = await response.Content.ReadFromJsonAsync<SubmissionResult>()
    ?? throw new InvalidOperationException("API khong tra ve ket qua nop bai.");
Console.WriteLine($"Da nop bai. Ma luot nop: {submitted.Id}");
Console.WriteLine($"Trang thai: {submitted.Status}");

var lookedUp = await http.GetFromJsonAsync<SubmissionResult>($"api/submissions/{submitted.Id}");
Console.WriteLine($"Tra cuu GET: {lookedUp?.Id} - {lookedUp?.Status}");

record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt);
