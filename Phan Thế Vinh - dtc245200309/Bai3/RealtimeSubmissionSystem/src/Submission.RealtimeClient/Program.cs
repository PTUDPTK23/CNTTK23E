using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiBaseUrl = args.Length > 0 && !args[0].StartsWith("--") ? args[0].TrimEnd('/') : "http://localhost:5000";
bool isDualMode = args.Any(a => a.Equals("--dual", StringComparison.OrdinalIgnoreCase))
                  || (args.Length >= 3 && !args[1].StartsWith("--") && !args[2].StartsWith("--"));

// Kênh 1: REST
using var http = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };

// Kênh 2: SignalR
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{apiBaseUrl}/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

if (isDualMode)
{
    // BÀI TẬP VẬN DỤNG: Theo dõi đồng thời hai lượt nộp trên cùng một kết nối SignalR
    var student1 = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "SV001";
    var student2 = args.Length > 2 && !args[2].StartsWith("--") ? args[2] : "SV002";

    var completionSources = new ConcurrentDictionary<Guid, TaskCompletionSource<string>>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
    {
        var shortId = m.SubmissionId.ToString()[..8];
        Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status}");

        if (m.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }

        if (completionSources.TryGetValue(m.SubmissionId, out var tcs))
        {
            if (m.Status is "graded" or "failed")
            {
                tcs.TrySetResult(m.Status);
            }
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault();
    if (selected is null)
    {
        Console.WriteLine("Khong tim thay bai tap.");
        return;
    }

    // Tạo lượt nộp 1 (hợp lệ -> graded)
    var res1 = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(student1, $"https://example.com/{student1.ToLower()}/signalr"));
    var sub1 = await res1.Content.ReadFromJsonAsync<SubmissionDto>();

    // Tạo lượt nộp 2 (không bắt đầu bằng https:// -> failed)
    var res2 = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(student2, $"http://example.com/{student2.ToLower()}/signalr"));
    var sub2 = await res2.Content.ReadFromJsonAsync<SubmissionDto>();

    if (sub1 is null || sub2 is null)
    {
        Console.WriteLine("Loi khi tao luot nop.");
        return;
    }

    var tcs1 = completionSources.GetOrAdd(sub1.Id, _ => new TaskCompletionSource<string>());
    var tcs2 = completionSources.GetOrAdd(sub2.Id, _ => new TaskCompletionSource<string>());

    Console.WriteLine($"Da tao {student1}: {sub1.Id.ToString()[..8]} ({sub1.Status})");
    Console.WriteLine($"Da tao {student2}: {sub2.Id.ToString()[..8]} ({sub2.Status})");

    // Watch cả hai trên cùng một kết nối SignalR
    await connection.InvokeAsync("WatchSubmission", sub1.Id.ToString());
    await connection.InvokeAsync("WatchSubmission", sub2.Id.ToString());

    // Chờ cả hai hoàn tất (tối đa 15 giây)
    await Task.WhenAll(
        Task.WhenAny(tcs1.Task, Task.Delay(TimeSpan.FromSeconds(15))),
        Task.WhenAny(tcs2.Task, Task.Delay(TimeSpan.FromSeconds(15)))
    );

    // Chốt lại bằng REST
    var rest1 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub1.Id}");
    var rest2 = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{sub2.Id}");

    var sigStatus1 = tcs1.Task.IsCompletedSuccessfully ? tcs1.Task.Result : "timeout";
    var sigStatus2 = tcs2.Task.IsCompletedSuccessfully ? tcs2.Task.Result : "timeout";

    Console.WriteLine($"{student1}: SignalR = {sigStatus1}, REST = {rest1?.Status}");
    Console.WriteLine($"{student2}: SignalR = {sigStatus2}, REST = {rest2?.Status}");
}
else
{
    // CHẾ ĐỘ TIÊU CHUẨN (Slide 55-57, Slide 71)
    var studentCode = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "SV001";
    var gradedNotification = new TaskCompletionSource<string>();

    connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", m =>
    {
        var shortId = m.SubmissionId.ToString().Length > 8 ? $"{m.SubmissionId.ToString()[..8]}..." : m.SubmissionId.ToString();
        var timeStr = m.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");
        Console.WriteLine($"Thong bao SignalR: {shortId} -> {m.Status} luc {timeStr}");

        if (m.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"!!! CANH BAO: luot nop {shortId} cham LOI (failed)");
            Console.ForegroundColor = oldColor;
        }

        if (m.Status is "graded" or "failed")
        {
            gradedNotification.TrySetResult(m.Status);
        }
    });

    await connection.StartAsync();
    Console.WriteLine("Da ket noi SignalR.");

    var assignments = await http.GetFromJsonAsync<List<AssignmentDto>>("/api/assignments");
    var selected = assignments?.FirstOrDefault();
    if (selected is null)
    {
        Console.WriteLine("Khong tim thay bai tap.");
        return;
    }
    Console.WriteLine($"Chon bai tap: {selected.Id} - {selected.Title}");

    var createResponse = await http.PostAsJsonAsync(
        $"/api/assignments/{selected.Id}/submissions",
        new CreateSubmissionRequest(studentCode, $"https://example.com/{studentCode.ToLower()}/signalr"));

    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var error = await createResponse.Content.ReadFromJsonAsync<ApiError>();
        Console.WriteLine($"Loi tao luot nop: {createResponse.StatusCode} - {error?.Code}: {error?.Message}");
        return;
    }

    var created = await createResponse.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Khong doc duoc du lieu luot nop tao moi.");
        return;
    }

    Console.WriteLine($"Da tao luot nop: {created.Id}");
    Console.WriteLine($"Trang thai REST ban dau: {created.Status}");
    Console.WriteLine("Dang cho thong bao trang thai tu may chu...");

    await connection.InvokeAsync("WatchSubmission", created.Id.ToString());

    var completed = await Task.WhenAny(gradedNotification.Task, Task.Delay(TimeSpan.FromSeconds(10)));
    if (completed != gradedNotification.Task)
    {
        Console.WriteLine("Qua thoi gian cho thong bao graded.");
    }

    var latest = await http.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{created.Id}");
    Console.WriteLine($"Trang thai doc lai bang REST: {latest?.Status}");
}

record AssignmentDto(int Id, string Title, string Description, DateTimeOffset Deadline);
record CreateSubmissionRequest(string? StudentCode, string? RepositoryUrl);
record SubmissionDto(Guid Id, int AssignmentId, string StudentCode, string RepositoryUrl, string Status, DateTimeOffset ReceivedAt, DateTimeOffset UpdatedAt);
record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
record ApiError(string Code, string Message);
