using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
var studentArg = args.Length > 1 ? args[1] : "SV001";

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

if (studentArg.Equals("ALL", StringComparison.OrdinalIgnoreCase))
{
    // === CHẾ ĐỘ BÀI TẬP 2 & 3: THEO DÕI NHIỀU LƯỢT NỘP VÀ XỬ LÝ TRẠNG THÁI FAILED ===
    var completionSources = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
    {
        if (m.Status == "failed")
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }
        else
        {
            Console.WriteLine($"Thong bao SignalR: {m.SubmissionId.ToString()[..8]} -> {m.Status}");
        }

        if (m.Status is "graded" or "failed")
        {
            if (completionSources.TryGetValue(m.SubmissionId, out var tcs))
            {
                tcs.TrySetResult(m.Status);
            }
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    // Tạo 2 lượt nộp: SV001 dùng https (graded), SV002 dùng http (failed)
    var resp1 = await http.PostAsJsonAsync("/api/assignments/1/submissions",
        new CreateSubmissionRequest("SV001", "https://github.com/sv001/signalr"));
    var sub1 = await resp1.Content.ReadFromJsonAsync<SubmissionDto>();
    completionSources[sub1!.Id] = new TaskCompletionSource<string>();
    Console.WriteLine($"Da tao SV001: {sub1.Id.ToString()[..8]} ({sub1.Status})");

    var resp2 = await http.PostAsJsonAsync("/api/assignments/1/submissions",
        new CreateSubmissionRequest("SV002", "http://github.com/sv002/signalr")); // http -> failed
    var sub2 = await resp2.Content.ReadFromJsonAsync<SubmissionDto>();
    completionSources[sub2!.Id] = new TaskCompletionSource<string>();
    Console.WriteLine($"Da tao SV002: {sub2.Id.ToString()[..8]} ({sub2.Status})");

    // Watch cả 2 trên cùng 1 kết nối
    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    // Chờ cả 2 hoàn tất
    await Task.WhenAll(completionSources[sub1.Id].Task, completionSources[sub2.Id].Task);

    // Đọc lại trạng thái từ REST
    var restSub1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var restSub2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    Console.WriteLine($"SV001: SignalR = {completionSources[sub1.Id].Task.Result}, REST = {restSub1?.Status}");
    Console.WriteLine($"SV002: SignalR = {completionSources[sub2.Id].Task.Result}, REST = {restSub2?.Status}");
}
else
{
    // === CHẾ ĐỘ TIÊU CHUẨN: 1 MÁY KHÁCH THEO DÕI 1 BÀI NỘP (Slide 55-57, Slide 71) ===
    var gradedNotification = new TaskCompletionSource(); // "cái chuông"

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
    {
        string timeStr = m.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
        Console.WriteLine($"Thong bao SignalR: {m.SubmissionId.ToString()[..8]}... -> {m.Status} luc {timeStr}");
        if (m.Status is "graded" or "failed")
        {
            if (m.Status == "failed")
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"!!! CANH BAO: luot nop {m.SubmissionId.ToString()[..8]}... cham LOI (failed)");
                Console.ForegroundColor = oldColor;
            }
            gradedNotification.TrySetResult();
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    // ① REST: Lấy danh sách bài tập
    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault();
    if (selected is null)
    {
        Console.WriteLine("Khong tim thay bai tap nao.");
        return;
    }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    // ② REST: Tạo lượt nộp
    var repoUrl = $"https://example.com/{studentArg.ToLower()}/signalr";
    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentArg, repoUrl));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop: {error?.Code} - {error?.Message}");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    Console.WriteLine($"Da tao luot nop: {created!.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

    // ③ SignalR: Đăng ký theo dõi lượt nộp (vào nhóm submission:{id})
    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    // ④ Chờ chuông graded hoặc timeout 10 giây
    var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != gradedNotification.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    // ⑤ REST: Đọc lại trạng thái chốt từ nguồn sự thật
    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

// Records
record AssignmentDto(int Id, string Title, string Description, DateTimeOffset DueDate);
record CreateSubmissionRequest(string StudentCode, string RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
