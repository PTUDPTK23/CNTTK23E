using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

if (args.FirstOrDefault()?.Equals("ping", StringComparison.OrdinalIgnoreCase) == true)
{
    return await PingDemo.RunAsync(args.Skip(1).ToArray());
}

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";
var simulateFailure = args.Length > 2 && args[2].Equals("fail", StringComparison.OrdinalIgnoreCase);

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
{
    Console.Error.WriteLine($"URL API không hợp lệ: {apiBaseUrl}");
    return 1;
}

using var http = new HttpClient { BaseAddress = baseUri };

List<AssignmentDto>? assignments;
try
{
    assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Không kết nối được API: {ex.Message}");
    return 1;
}

if (assignments is null || assignments.Count == 0)
{
    Console.WriteLine("Hiện chưa có bài tập.");
    return 0;
}

Console.WriteLine("Danh sách bài tập:");
foreach (var assignment in assignments)
{
    Console.WriteLine($"{assignment.Id}. {assignment.Title} - {assignment.Description}");
}

Console.Write($"Chọn bài tập (mặc định {assignments[0].Id}): ");
var selection = Console.ReadLine();
var assignmentId = int.TryParse(selection, out var selectedId)
    ? selectedId
    : assignments[0].Id;
var selectedAssignment = assignments.FirstOrDefault(item => item.Id == assignmentId);
if (selectedAssignment is null)
{
    Console.Error.WriteLine("Mã bài tập không có trong danh sách.");
    return 1;
}

var statusReceived = new TaskCompletionSource<SubmissionStatusMessage>(
    TaskCreationOptions.RunContinuationsAsynchronously);

await using var connection = new HubConnectionBuilder()
    .WithUrl(new Uri(baseUri, "/hubs/submissions"))
    .WithAutomaticReconnect()
    .Build();

connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message =>
{
    Console.WriteLine($"SignalR: {message.SubmissionId} -> {message.Status} lúc {message.UpdatedAt:HH:mm:ss}");
    if (message.Status is "graded" or "failed")
    {
        statusReceived.TrySetResult(message);
    }
});

connection.Reconnecting += error =>
{
    Console.WriteLine($"[Lifecycle] Đang kết nối lại: {error?.Message}");
    return Task.CompletedTask;
};
connection.Reconnected += connectionId =>
{
    Console.WriteLine($"[Lifecycle] Đã kết nối lại: {connectionId}");
    return Task.CompletedTask;
};
connection.Closed += error =>
{
    Console.WriteLine($"[Lifecycle] Kết nối đã đóng: {error?.Message}");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine("Đã kết nối SignalR.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Không thể kết nối SignalR: {ex.Message}");
    return 1;
}

var request = new CreateSubmissionRequest(
    studentCode,
    $"https://example.com/{Uri.EscapeDataString(studentCode)}/signalr{(simulateFailure ? "/fail" : "")}");

using var createResponse = await http.PostAsJsonAsync(
    $"/api/assignments/{selectedAssignment.Id}/submissions", request);

if (createResponse.StatusCode != HttpStatusCode.Created)
{
    var errorBody = await createResponse.Content.ReadAsStringAsync();
    Console.Error.WriteLine($"Tạo lượt nộp thất bại ({(int)createResponse.StatusCode}): {errorBody}");
    return 1;
}

var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
if (created is null)
{
    Console.Error.WriteLine("API không trả về dữ liệu lượt nộp.");
    return 1;
}

Console.WriteLine($"Đã tạo lượt nộp: {created.Id}");
Console.WriteLine($"Trạng thái REST ban đầu: {created.Status}");

await connection.InvokeAsync("WatchSubmission", created.Id.ToString());
Console.WriteLine("Đang chờ thông báo trạng thái từ máy chủ...");

var completed = await Task.WhenAny(statusReceived.Task, Task.Delay(TimeSpan.FromSeconds(15)));
if (completed != statusReceived.Task)
{
    Console.WriteLine("Quá thời gian chờ thông báo hoàn tất.");
}

var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
Console.WriteLine($"Trạng thái đọc lại bằng REST: {latest?.Status ?? "không xác định"}");
return 0;

public sealed record AssignmentDto(int Id, string Title, string Description);
public sealed record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
public sealed record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);
public sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
