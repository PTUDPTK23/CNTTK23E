// Bài 3 – EduSubmit lát cắt 3 (PI 3.3): máy khách REST + SignalR (giáo trình Mã nguồn 3.4)
// Chạy: dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentCode = args.Length > 1 ? args[1] : "SV001";

// Kênh 1 – REST (giống Bài 2)
using var http = new HttpClient {
    BaseAddress = new Uri(apiBaseUrl)
};

// Kênh 2 – SignalR: kết nối lâu dài tới hub
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

// "Cái chuông": hoàn tất khi nhận được trạng thái graded
var gradedNotification = new TaskCompletionSource();

// Đăng ký hàm xử lý TRƯỚC khi mở kết nối để không lỡ thông điệp
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", message => {
    Console.WriteLine(
        $"Thong bao SignalR: {message.SubmissionId} -> {message.Status} luc {message.UpdatedAt.ToLocalTime():HH:mm:ss}");
    if (message.Status == "graded") {
        gradedNotification.TrySetResult();
    }
    else if (message.Status == "failed") {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"!!! CANH BAO: luot nop {message.SubmissionId} cham LOI (failed)");
        Console.ForegroundColor = prev;
        gradedNotification.TrySetResult();
    }
});

await connection.StartAsync();
Console.WriteLine("Da ket noi SignalR.");

// REST: lấy danh sách bài tập
var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
var selectedAssignment = assignments?.FirstOrDefault();
if (selectedAssignment is null) {
    Console.WriteLine("Khong co bai tap nao de nop.");
    return;
}
Console.WriteLine($"Chon bai tap: {selectedAssignment.Id} - {selectedAssignment.Title}");

// REST: tạo lượt nộp
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

// SignalR: xin vào nhóm submission:{id} để nhận thông báo của RIÊNG lượt nộp này
await connection.InvokeAsync("WatchSubmission", createdSubmission.Id.ToString());
Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

// Chờ "chuông" graded, tối đa 10 giây
var completed = await Task.WhenAny(
    gradedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != gradedNotification.Task) {
    Console.WriteLine("Qua thoi gian cho thong bao graded.");
}

// REST là nguồn sự thật: đọc lại trạng thái cuối
var latestSubmission = await http.GetFromJsonAsync<SubmissionDto>(
    $"/api/submissions/{createdSubmission.Id}");
Console.WriteLine($"Trang thai doc lai bang REST: {latestSubmission?.Status}");

static async Task PrintApiErrorAsync(HttpResponseMessage response) {
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"{(int)response.StatusCode} {response.StatusCode}: {error?.Code} - {error?.Message}");
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
