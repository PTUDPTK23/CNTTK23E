using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

const string apiBase = "http://localhost:5000";
const string hubUrl = apiBase + "/hubs/submissions";

using var http = new HttpClient { BaseAddress = new Uri(apiBase) };
await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

var latestStatus = new Dictionary<Guid, string>();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    latestStatus[message.SubmissionId] = message.Status;
    Console.WriteLine($"SignalR: {message.SubmissionId} -> {message.Status} at {message.UpdatedAt:O}");
});

connection.Reconnected += async _ =>
{
    Console.WriteLine("SignalR reconnected; client should Watch again and GET REST state.");
    await Task.CompletedTask;
};

try
{
    // On(...) is registered above, before StartAsync().
    await connection.StartAsync();
    Console.WriteLine($"Da ket noi SignalR. ConnectionId: {connection.ConnectionId}");

    var request = new CreateSubmissionRequest(
        "SV001", "https://example.com/sv001/signalr");

    var response = await http.PostAsJsonAsync("/api/assignments/1/submissions", request);
    Console.WriteLine($"POST status: {(int)response.StatusCode}");
    response.EnsureSuccessStatusCode();

    var created = await response.Content.ReadFromJsonAsync<SubmissionDto>()
        ?? throw new InvalidOperationException("Không đọc được response POST.");

    Console.WriteLine($"Da tao {created.StudentCode}: {created.Id} ({created.Status})");

    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
    Console.WriteLine($"Watching group submission:{created.Id}");

    Console.WriteLine("Đang chờ các thông báo trạng thái...");
    await Task.Delay(TimeSpan.FromSeconds(4500));

    var finalState = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"REST final status: {finalState?.Status}");
    Console.WriteLine($"SignalR last status: {latestStatus.GetValueOrDefault(created.Id, "(no event received)")}");
}
catch (Exception ex)
{
    Console.WriteLine($"Client error: {ex.Message}");
    Console.WriteLine("Hãy chắc chắn Submission.RealtimeApi đang chạy ở cổng 5000.");
}

public sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public sealed record SubmissionDto(
    Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl,
    string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
public sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
