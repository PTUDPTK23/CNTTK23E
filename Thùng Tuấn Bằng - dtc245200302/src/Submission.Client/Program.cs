
using System.Net.Http.Json;

var apiUrl = args.Length > 0
    ? args[0]
    : "http://localhost:5092";

using var client = new HttpClient
{
    BaseAddress = new Uri(apiUrl)
};

var assignments =
    await client.GetFromJsonAsync<List<Assignment>>(
        "/api/assignments");

Console.WriteLine("Danh sach bai tap:");

foreach (var item in assignments ?? new())
{
    Console.WriteLine($"{item.Id}: {item.Title}");
}

var request = new CreateSubmissionRequest(
    1,
    "DTC245200302",
    "https://github.com/example/EduSubmit"
);

var response = await client.PostAsJsonAsync(
    "/api/submissions", request);

response.EnsureSuccessStatusCode();

var result =
    await response.Content.ReadFromJsonAsync<SubmissionResult>();

Console.WriteLine($"Da nop bai. Ma luot nop: {result?.Id}");
Console.WriteLine($"Trang thai: {result?.Status}");

if (result != null)
{
    var saved = await client.GetFromJsonAsync<SubmissionResult>(
        $"/api/submissions/{result.Id}");

    Console.WriteLine($"Doc lai ma luot nop: {saved?.Id}");
    Console.WriteLine($"Trang thai doc lai: {saved?.Status}");
}

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
