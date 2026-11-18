
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1
    ? args[1]
    : "SV001";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

var gradedNotification = new TaskCompletionSource<bool>(
    TaskCreationOptions.RunContinuationsAsynchronously);

// Đăng ký nhận sự kiện trước khi kết nối
connection.On<SubmissionStatusMessage>(
    "SubmissionStatusChanged",
    message =>
    {
        Console.WriteLine(
            $"Thong bao SignalR: {message.SubmissionId} -> {message.Status}");

        if (message.Status == "graded")
        {
            gradedNotification.TrySetResult(true);
        }
    });

// Kết nối SignalR
await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// 1. Lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>(
    "/api/assignments");

var selected = assignments?.FirstOrDefault();

if (selected is null)
{
    Console.WriteLine("Khong co bai tap nao.");
    return;
}

Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

// 2. Tạo lượt nộp bài
var request = new CreateSubmissionRequest(
    studentCode,
    $"https://example.com/{studentCode}/signalr");

var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selected.Id}/submissions",
    request);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine(
        $"Tao luot nop that bai: {error?.Code} - {error?.Message}");
    return;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();

if (created is null)
{
    Console.WriteLine("Khong doc duoc ket qua tao bai nop.");
    return;
}

Console.WriteLine($"Da tao luot nop: {created.Id}");
Console.WriteLine($"Trang thai REST ban dau: {created.Status}");

// 3. Theo dõi lượt nộp qua SignalR
await connection.InvokeAsync(
    "WatchSubmission",
    created.Id.ToString());

Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// 4. Chờ trạng thái graded, tối đa 10 giây
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task)
{
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// 5. Đọc lại trạng thái mới nhất bằng REST
var latest = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{created.Id}");

Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");

// Các kiểu dữ liệu phía Client
public record AssignmentDto(
    int Id,
    string Title,
    string Description,
    bool IsOpen);

public record CreateSubmissionRequest(
    string StudentCode,
    string RepositoryUrl);

public record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);

public record SubmissionStatusMessage(
    Guid SubmissionId,
    string Status,
    DateTimeOffset UpdatedAt);

public record ApiError(
    string Code,
    string Message);
