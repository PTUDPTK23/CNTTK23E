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

var gradedNotification = new TaskCompletionSource();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message => {
    Console.WriteLine($"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "graded") {
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignments?.FirstOrDefault();
if (selectedAssignment is null) {
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}
Console.WriteLine($"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

var createRequest = new CreateSubmissionRequest(
    StudentCode: studentCode,
    RepositoryUrl: $"https://example.com/{studentCode.ToLower()}/signalr");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignment.Id}/submissions",
    createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created) {
    await PrintApiErrorAsync(createResponse);
    return;
}

var createdSubmission = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (createdSubmission is null) {
    Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {createdSubmission.Id}");
Console.WriteLine($"Trang thai REST ban dau: {createdSubmission.Status}");

await connection.InvokeAsync("WatchSubmission", createdSubmission.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task) {
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

var latestSubmission = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{createdSubmission.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latestSubmission?.Status}");

Console.WriteLine("Nhan Enter de thoat.");
Console.ReadLine();

static async Task PrintApiErrorAsync(HttpResponseMessage response) {
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
}

record AssignmentDto(int Id, string Title, string Description, bool IsOpen);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);