using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var hubUrl = "http://localhost:5000/hubs/submissions";
var apiBaseUrl = "http://localhost:5000";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

var gradedTcs = new TaskCompletionSource<bool>();

// Lắng nghe sự kiện cập nhật trạng thái từ SignalR Hub
connection.On<StatusUpdateMessage>("SubmissionStatusChanged", update =>
{
    var timeStr = update.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
    if (update.Status == "processing")
    {
        Console.WriteLine($"[SignalR] Nhận thông báo: status = {update.Status} (lúc {timeStr})");
    }
    else if (update.Status == "graded")
    {
        Console.WriteLine($"[SignalR] Nhận thông báo: status = {update.Status} (lúc {timeStr})");
        Console.WriteLine($"[SignalR] Điểm: {update.Grade}/10 - Nhận xét: {update.Feedback}");
        gradedTcs.TrySetResult(true);
    }
});

try
{
    // 1. Kết nối SignalR Hub
    await connection.StartAsync();
    Console.WriteLine("[Client] Kết nối SignalR Hub thành công");

    // 2. Gửi yêu cầu nộp bài qua REST API
    using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    var payload = new
    {
        studentCode = "dtc245200476",
        studentName = "Nguyễn Xuân Hợp",
        repoUrl = "https://github.com/nguyenxuanhoppt478-cmyk/CNTTK23E.git"
    };

    var postResponse = await httpClient.PostAsJsonAsync("/api/assignments/3/submissions", payload);
    postResponse.EnsureSuccessStatusCode();

    var submission = await postResponse.Content.ReadFromJsonAsync<SubmissionResponse>();
    if (submission == null)
    {
        throw new InvalidOperationException("Không thể giải mã dữ liệu phản hồi bài nộp.");
    }

    Console.WriteLine($"[REST] Nộp bài thành công: id = {submission.Id}, status = {submission.Status}");

    // 3. Tham gia nhóm SignalR để theo dõi tiến trình
    await connection.InvokeAsync("WatchSubmission", submission.Id.ToString());
    Console.WriteLine($"[SignalR] Đã tham gia nhóm theo dõi: submission:{submission.Id}");

    // 4. Chờ nhận sự kiện chấm bài xong
    var completedTask = await Task.WhenAny(gradedTcs.Task, Task.Delay(15000));
    if (completedTask != gradedTcs.Task)
    {
        Console.WriteLine("[Cảnh báo] Quá thời gian chờ chấm bài!");
    }

    // 5. Đọc lại bài nộp qua REST API
    var finalResponse = await httpClient.GetFromJsonAsync<SubmissionResponse>($"/api/submissions/{submission.Id}");
    if (finalResponse != null)
    {
        Console.WriteLine($"[REST] Đọc lại bài nộp: status = {finalResponse.Status}, grade = {finalResponse.Grade}");
    }

    // 6. Hoàn thành
    Console.WriteLine("[Client] Hoàn thành quy trình nộp và theo dõi bài tập");
}
catch (Exception ex)
{
    Console.WriteLine($"[Lỗi Client]: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}

public record StatusUpdateMessage(
    Guid SubmissionId,
    string Status,
    double? Grade,
    string? Feedback,
    DateTime UpdatedAt
);

public class SubmissionResponse
{
    public Guid Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string RepoUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
