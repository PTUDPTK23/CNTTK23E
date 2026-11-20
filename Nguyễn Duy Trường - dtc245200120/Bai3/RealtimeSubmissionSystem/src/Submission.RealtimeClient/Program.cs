
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/')
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

// Cai chuong cho trang thai graded
var gradedNotification = new TaskCompletionSource();

// Dang ky nhan thong bao
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged", m =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {m.SubmissionId} -> {m.Status} " +
        $"luc {m.UpdatedAt.ToLocalTime():HH:mm:ss}");

    if (m.Status == "graded")
        gradedNotification.TrySetResult();
});

// Mo ket noi SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. REST: Lay danh sach bai tap
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");

var selected = assignments?.FirstOrDefault();

if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}

Console.WriteLine(
    $"Chon bai tap: {selected.Id} - {selected.Title}");

// 2. REST: Tao luot nop
using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode,
        $"https://example.com/{studentCode}/signalr"));

// Kiem tra HTTP 201 truoc khi doc body
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"Loi tao luot nop: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("Khong doc duoc luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. SignalR: Theo doi dung luot nop
await connection.InvokeAsync(
    "WatchSubmission", created.Id.ToString());

// 4. Cho thong bao graded hoac timeout 10 giay
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. REST: Doc lai trang thai cuoi
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}");

// DTO
record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsActive);

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
