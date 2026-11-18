
using System.Net.Http.Json;

// Dia chi API co the thay doi khi chay Client
var apiBaseUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5200";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<Assignment>>(
    "/api/assignments");

Console.WriteLine("DANH SACH BAI TAP:");

foreach (var assignment in assignments ?? new List<Assignment>())
{
    Console.WriteLine($"- {assignment.Id}: {assignment.Title}");
}

// Gui bai nop
var request = new CreateSubmissionRequest(
    AssignmentId: 1,
    StudentCode: "DTC245200988",
    RepositoryUrl: "https://github.com/dtc245200988-byte/EduSubmit-DTC245200988"
);

var response = await http.PostAsJsonAsync("/api/submissions", request);
response.EnsureSuccessStatusCode();

var result = await response.Content
    .ReadFromJsonAsync<SubmissionResult>();

Console.WriteLine();
Console.WriteLine("NOP BAI THANH CONG!");
Console.WriteLine($"Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

record Assignment(int Id, string Title, string Description);

record CreateSubmissionRequest(
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl);

record SubmissionResult(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string Status,
    DateTimeOffset ReceivedAt);
