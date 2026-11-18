using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

Console.WriteLine("=== EduSubmit Realtime Client ===");

// Nhập ID của lượt nộp cần theo dõi từ bàn phím hoặc gán cứng ID thử nghiệm
Console.Write("Nhập Submission ID cần theo dõi realtime: ");
var submissionId = Console.ReadLine()?.Trim();

if (string.IsNullOrEmpty(submissionId)) {
    Console.WriteLine("ID không được để trống!");
    return;
}

// Kết nối tới Hub SignalR của Server
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/submissions")
    .WithAutomaticReconnect()
    .Build();

using var httpClient = new HttpClient {
    BaseAddress = new Uri("http://localhost:5000")
};

// SignalR chỉ dùng để nhận thông báo phát sinh; REST được gọi lại để lấy trạng thái chính xác.
connection.On<SubmissionStatusMessage>("SubmissionStatusChanged", async message => {
    Console.WriteLine();

    if (string.Equals(message.Status, "failed", StringComparison.OrdinalIgnoreCase)) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CANH BAO] Submission {message.SubmissionId} FAILED: RepositoryUrl khong bat dau bang https://");
        Console.ResetColor();
    } else {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[NHAN THONG BAO REALTIME] Submission {message.SubmissionId} da doi trang thai thanh: {message.Status}");
        Console.ResetColor();
    }

    await RefreshFromRestAsync(httpClient, message.SubmissionId);
});

try {
    await connection.StartAsync();
    Console.WriteLine($"Đã kết nối thành công tới Hub! Đang theo dõi nhóm submission:{submissionId}...");

    // Gọi phương thức WatchSubmission trên Server để join vào group tương ứng
    await connection.InvokeAsync("WatchSubmission", submissionId);
}
catch (Exception ex) {
    Console.WriteLine($"Lỗi kết nối: {ex.Message}");
    return;
}

Console.WriteLine("Nhấn Enter để thoát ứng dụng...");
Console.ReadLine();

static async Task RefreshFromRestAsync(HttpClient httpClient, Guid submissionId) {
    try {
        var submission = await httpClient.GetFromJsonAsync<SubmissionDto>($"/api/submissions/{submissionId}");
        if (submission is null) {
            Console.WriteLine("[REST] Khong doc duoc thong tin submission.");
            return;
        }

        Console.WriteLine($"[REST] Trang thai hien tai: {submission.Status}; cap nhat luc: {submission.UpdatedAt:u}");
    } catch (HttpRequestException ex) {
        Console.WriteLine($"[REST] Loi doc trang thai: {ex.Message}");
    }
}

sealed record SubmissionStatusMessage(Guid SubmissionId, string Status, DateTimeOffset UpdatedAt);
sealed record SubmissionDto(
    Guid Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset ReceivedAt,
    DateTimeOffset UpdatedAt);