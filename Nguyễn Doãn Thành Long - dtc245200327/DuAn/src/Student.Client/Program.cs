using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;

Console.OutputEncoding = Encoding.UTF8;

var apiUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5050";
var notificationUrl = args.Length > 1 ? args[1].TrimEnd('/') : "http://localhost:5001";
var studentId = args.Length > 2 ? args[2] : "SV001";

using var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

// Kết nối tới Notification Service qua SignalR để nhận thông báo realtime (Bước 8)
await using var hubConnection = new HubConnectionBuilder()
    .WithUrl($"{notificationUrl}/hubs/notifications")
    .WithAutomaticReconnect()
    .Build();

var gradedTcs = new TaskCompletionSource<SubmissionGradedEvent>();

hubConnection.On<SubmissionGradedEvent>("SubmissionGraded", evt =>
{
    Console.WriteLine();
    Console.WriteLine("============================================================");
    Console.WriteLine("[Bước 8 - REALTIME NOTIFICATION TỪ NOTIFICATION SERVICE]");
    Console.WriteLine($"  SubmissionId : {evt.SubmissionId}");
    Console.WriteLine($"  StudentId    : {evt.StudentId}");
    Console.WriteLine($"  Status       : {evt.Status}");
    Console.WriteLine($"  Score        : {evt.Score}");
    Console.WriteLine($"  Comment      : {evt.Comment}");
    Console.WriteLine("============================================================");
    gradedTcs.TrySetResult(evt);
});

await hubConnection.StartAsync();
await hubConnection.InvokeAsync("JoinStudentGroup", studentId);
Console.WriteLine($"[Student.Client] Đã kết nối Notification Service ({notificationUrl}) cho sinh viên '{studentId}'.");

// Bước 1: Tra cứu danh sách bài tập (GET /assignments)
Console.WriteLine();
Console.WriteLine("--- [Bước 1] Gọi GET /assignments để lấy danh sách bài tập ---");
var assignments = await http.GetFromJsonAsync<List<Assignment>>("/assignments") ?? [];
foreach (var a in assignments)
{
    Console.WriteLine($"  - [{a.Id}] {a.Title}: {a.Description}");
}

if (assignments.Count == 0)
{
    Console.WriteLine("Không có bài tập nào trong hệ thống.");
    return;
}

var selectedAssignment = assignments[0];

// Bước 2: Nộp bài cho một bài tập (POST /assignments/{id}/submissions)
Console.WriteLine();
Console.WriteLine($"--- [Bước 2] Gọi POST /assignments/{selectedAssignment.Id}/submissions ---");
var payload = new
{
    studentId,
    content = $"https://github.com/{studentId.ToLowerInvariant()}/assignment-{selectedAssignment.Id}-solution-pass"
};

var postResponse = await http.PostAsJsonAsync($"/assignments/{selectedAssignment.Id}/submissions", payload);
postResponse.EnsureSuccessStatusCode();

// Bước 3 & 4: Nhận phản hồi bài nộp với trạng thái Submitted
var createdSubmission = await postResponse.Content.ReadFromJsonAsync<Submission>();
Console.WriteLine("[Bước 3 & 4] Đã tạo bài nộp và đẩy yêu cầu chấm vào RabbitMQ:");
Console.WriteLine($"  Id           : {createdSubmission!.Id}");
Console.WriteLine($"  AssignmentId : {createdSubmission.AssignmentId}");
Console.WriteLine($"  StudentId    : {createdSubmission.StudentId}");
Console.WriteLine($"  Content      : {createdSubmission.Content}");
Console.WriteLine($"  SubmittedAt  : {createdSubmission.SubmittedAt:O}");
Console.WriteLine($"  Status       : {createdSubmission.Status}");

// Đợi Grading Worker chấm tự động (Bước 5, 6, 7) và Notification Service đẩy thông báo (Bước 8)
Console.WriteLine();
Console.WriteLine("[Bước 5 - 7] Đang chờ Grading Worker chấm điểm tự động và gửi thông báo realtime...");
var completedTask = await Task.WhenAny(gradedTcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
if (completedTask != gradedTcs.Task)
{
    Console.WriteLine("Quá thời gian chờ thông báo realtime (15 giây).");
}

// Bước 8 (tiếp): Tra cứu lại chi tiết bài nộp qua API (GET /submissions/{id})
Console.WriteLine();
Console.WriteLine($"--- [Bước 8] Gọi GET /submissions/{createdSubmission.Id} để tra cứu chi tiết bài nộp ---");
var submissionDetail = await http.GetFromJsonAsync<Submission>($"/submissions/{createdSubmission.Id}");
if (submissionDetail is not null)
{
    Console.WriteLine($"  Id           : {submissionDetail.Id}");
    Console.WriteLine($"  AssignmentId : {submissionDetail.AssignmentId}");
    Console.WriteLine($"  StudentId    : {submissionDetail.StudentId}");
    Console.WriteLine($"  Content      : {submissionDetail.Content}");
    Console.WriteLine($"  SubmittedAt  : {submissionDetail.SubmittedAt:O}");
    Console.WriteLine($"  Status       : {submissionDetail.Status}");
    Console.WriteLine($"  Score        : {submissionDetail.Score}");
    Console.WriteLine($"  Comment      : {submissionDetail.Comment}");
}

// Tra cứu danh sách bài đã nộp của sinh viên (GET /students/{studentId}/submissions)
Console.WriteLine();
Console.WriteLine($"--- [Bước 8] Gọi GET /students/{studentId}/submissions để tra cứu lịch sử nộp bài ---");
var studentSubmissions = await http.GetFromJsonAsync<List<Submission>>($"/students/{studentId}/submissions") ?? [];
foreach (var s in studentSubmissions)
{
    Console.WriteLine($"  - Submission #{s.Id} | Assignment #{s.AssignmentId} | Status: {s.Status} | Score: {s.Score} | Comment: {s.Comment}");
}
