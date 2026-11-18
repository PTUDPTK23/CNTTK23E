using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

// Cau hinh dia chi may chu va ma sinh vien
var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "SV001";

// Kenh 1: REST API
using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

// Kenh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// Cho thong bao cham bai xong
var gradedNotification = new TaskCompletionSource();

// Dang ky nhan thong bao tu may chu
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged", message =>
{
    Console.WriteLine(
        $"Thong bao SignalR: {message.Id} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");

    if (message.Status == "graded")
    {
        gradedNotification.TrySetResult();
    }
});

// Ket noi SignalR
await connection.StartAsync();

Console.WriteLine("Da ket noi SignalR.");

// Buoc 6.1: Lay danh sach bai tap bang REST
var assignments =
    await http.GetFromJsonAsync<List<AssignmentDto>>(
        "/api/assignments");

var selected = assignments?.FirstOrDefault();

if (selected is null)
{
    Console.WriteLine("Khong co bai tap.");
    return;
}

Console.WriteLine(
    $"Chon bai tap: {selected.Id} - {selected.Title}");

// Buoc 6.2: Tao luot nop bai bang REST
var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    new CreateSubmissionRequest(
        studentCode,
        $"https://example.com/{studentCode}/signalr"));

// Kiem tra ma trang thai HTTP
if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content
        .ReadFromJsonAsync<ApiError>();

    Console.WriteLine(
        $"Loi: {error?.Code} - {error?.Message}");

    return;
}

// Doc thong tin luot nop vua tao
var created = await createResponse.Content
    .ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("Khong doc duoc luot nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");

Console.WriteLine(
    $"Trang thai REST ban dau: {created.Status}");

// Buoc 6.3: Tham gia nhom theo doi luot nop
await connection.InvokeAsync(
    "WatchSubmission", created.Id.ToString());

Console.WriteLine(
    "Dang cho thong bao trang thai tu may chu...");

// Buoc 6.4: Cho thong bao graded, toi da 10 giay
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine(
        "Qua thoi gian cho thong bao graded.");
}

// Buoc 6.5: Doc lai trang thai cuoi bang REST
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");

Console.WriteLine(
    $"Trang thai doc lai bang REST: {latest?.Status}");

// Cac DTO
record AssignmentDto(
    int Id,
    string Title,
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
    Guid Id,
    string Status,
    DateTimeOffset UpdatedAt);

record ApiError(
    string Code,
    string Message);
