using System.Net.Http.Json;

var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine("--- Lay danh sach bai tap ---");
var assignments = await client.GetFromJsonAsync<List<Assignment>>("/api/assignments");
if (assignments != null)
{
    foreach (var item in assignments)
    {
        Console.WriteLine($"  - {item.Id}: {item.Title} ({item.Description})");
    }
}

Console.WriteLine("\n--- Gui bai nop ---");
var request = new CreateSubmissionRequest(1, "DTC245200019", "https://github.com/example/repo");
var response = await client.PostAsJsonAsync("/api/submissions", request);

response.EnsureSuccessStatusCode();

var result = await response.Content.ReadFromJsonAsync<SubmissionResult>();
if (result != null)
{
    Console.WriteLine($"Da nop bai. Ma luot nop: {result.Id} | Trang thai: {result.Status}");

    Console.WriteLine("\n--- Doc lai luot nop vua tao ---");
    var fetchedSub = await client.GetFromJsonAsync<SubmissionResult>($"/api/submissions/{result.Id}");
    if (fetchedSub != null)
    {
        Console.WriteLine($"Kiem tra lai API -> ID: {fetchedSub.Id}, SV: {fetchedSub.StudentCode}, Thoi gian: {fetchedSub.ReceivedAt}");
    }
}

record Assignment(int Id, string Title, string Description);
record CreateSubmissionRequest(int AssignmentId, string StudentCode, string RepositoryUrl);
record SubmissionResult(Guid Id, int AssignmentId, string StudentCode, string Status, DateTimeOffset ReceivedAt);