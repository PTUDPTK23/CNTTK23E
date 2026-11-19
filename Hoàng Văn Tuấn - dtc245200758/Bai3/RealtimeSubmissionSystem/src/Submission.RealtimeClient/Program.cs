using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource(
    TaskCreationOptions.RunContinuationsAsynchronously);
string? lastSubmissionId = null;

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {Short(message.SubmissionId)} -> {message.Status}");
    if (message.Status == "graded")
        gradedNotification.TrySetResult();
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("Dang ket noi lai SignalR...");
    return Task.CompletedTask;
};
connection.Reconnected += async _ =>
{
    Console.WriteLine("Da ket noi lai SignalR.");
    // A new connection has no old group membership; re-watch after reconnect.
    if (lastSubmissionId is not null)
        await connection.InvokeAsync("WatchSubmission", lastSubmissionId);
};

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selected = assignments?.FirstOrDefault();
if (selected is null)
{
    Console.WriteLine("Khong co bai tap de nop.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode, $"https://example.com/{studentCode}/signalr"));

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"Tao luot nop that bai: {(int)createResponse.StatusCode} {error}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null)
{
    Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
    return;
}

lastSubmissionId = created.Id.ToString();
Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

await connection.InvokeAsync("WatchSubmission", lastSubmissionId);
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
    Console.WriteLine("Qua thoi gian cho thong bao graded.");

var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

static string Short(Guid id) => id.ToString()[..8];

record AssignmentDto(int Id, string Title, string Status, DateTimeOffset CreatedAt);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(
    Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
