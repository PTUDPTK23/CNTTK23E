
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kenh 1: REST
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource();

// Dang ky lang nghe truoc khi ket noi
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> {message.Status}");

        if (message.Status == "graded")
            gradedNotification.TrySetResult();
    });

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// Lay danh sach bai tap
var assignments =
    await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments");

var selectedAssignment = assignments?.FirstOrDefault();

if (selectedAssignment is null)
{
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}

Console.WriteLine(
    $"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

// Tao luot nop
var createRequest = new CreateSubmissionRequest(
    studentCode,
    $"https://example.com/{studentCode.ToLower()}/signalr");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignment.Id}/submissions",
    createRequest);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    await PrintApiErrorAsync(createResponse);
    return;
}

var createdSubmission =
    await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

if (createdSubmission is null)
{
    Console.WriteLine("Khong doc duoc phan hoi tao luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {createdSubmission.Id}");
Console.WriteLine($"Trang thai REST ban dau: {createdSubmission.Status}");

// Dang ky nhom theo ID

await connection.InvokeAsync(
    "WatchSubmission",
    createdSubmission.Id.ToString());

Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Cho graded toi da 10 giay
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// Doc lai trang thai bang REST
var latestSubmission =
    await http.GetFromJsonAsync<SubmissionDto>(
        $"/api/submissions/{createdSubmission.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latestSubmission?.Status}");

static async Task PrintApiErrorAsync(HttpResponseMessage response)
{
    var error = await response.Content.ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"{(int)response.StatusCode} {response.StatusCode}: " +
        $"{error?.Code} - {error?.Message}");
}

record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
