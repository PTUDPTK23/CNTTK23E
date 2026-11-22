using System;
using System.Text;
using System.Threading.Tasks;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = Encoding.UTF8;

var hubUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5024/hubs/dashboard";

Console.WriteLine("===============================================================================");
Console.WriteLine("        EVENTFLOW REALTIME DASHBOARD CLIENT (SIGNALR - TUẦN 2)                ");
Console.WriteLine("        Sinh viên: Phan Quang Tùng - MSSV: DTC245200250                        ");
Console.WriteLine("        Học phần : Phát triển ứng dụng phân tán (CNTT K23E)                   ");
Console.WriteLine("===============================================================================");
Console.WriteLine($"[1] Đang kết nối tới SignalR Hub: {hubUrl}...");

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
    .Build();

connection.Reconnecting += error =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[RECONNECTING] Kết nối SignalR bị gián đoạn ({error?.Message}). Đang thử kết nối lại...");
    Console.ResetColor();
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[RECONNECTED] Đã kết nối lại thành công! ConnectionId mới: {connectionId}");
    Console.ResetColor();
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[CLOSED] Kết nối SignalR đã đóng ({error?.Message}).");
    Console.ResetColor();
    return Task.CompletedTask;
};

// Đăng ký nhận thống kê tổng hợp (KPI Stats) từ Hub
connection.On<DashboardStatsDto>("ReceiveDashboardStats", stats =>
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n------------------- [SIGNALR BROADCAST: SỐ LIỆU DASHBOARD] -------------------");
    Console.WriteLine($" Thời gian : {stats.LastUpdated.ToLocalTime():HH:mm:ss dd/MM/yyyy}");
    Console.WriteLine($" Sự kiện   : {stats.TotalEvents} sự kiện đang mở");
    Console.WriteLine($" Đăng ký   : {stats.TotalRegistrations} vé tham dự");
    Console.WriteLine($" Check-in  : {stats.TotalCheckedIn} người ({stats.OverallCheckInRate}% tỷ lệ có mặt)");
    Console.WriteLine($" Chỗ trống : {stats.TotalRemainingSpots} chỗ còn lại");
    Console.WriteLine("--------------------------------------------------------------------------------");
    Console.ResetColor();
});

// Đăng ký nhận thông báo luồng hoạt động thời gian thực (Activity Log) từ Hub
connection.On<ActivityLogDto>("ReceiveEventActivity", activity =>
{
    var color = activity.EventType switch
    {
        "ParticipantRegistered" => ConsoleColor.Green,
        "ParticipantCheckedIn" => ConsoleColor.Magenta,
        "ParticipantCancelled" => ConsoleColor.DarkYellow,
        _ => ConsoleColor.White
    };

    Console.ForegroundColor = color;
    Console.WriteLine($"\n[REALTIME EVENT - {activity.EventType}]");
    Console.WriteLine($"  Mô tả   : {activity.Description}");
    Console.WriteLine($"  Sự kiện : {activity.EventName}");
    Console.WriteLine($"  Tham dự : {activity.ParticipantName}");
    Console.WriteLine($"  Mã luồng: {activity.CorrelationId}");
    Console.WriteLine($"  Thời gian: {activity.Timestamp.ToLocalTime():HH:mm:ss}");
    Console.ResetColor();
});

try
{
    await connection.StartAsync();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"[2] Kết nối SignalR thành công! ConnectionId: {connection.ConnectionId}");
    Console.WriteLine("[3] Đang lắng nghe các sự kiện thời gian thực từ EventFlow.DashboardService...");
    Console.WriteLine("    (Nhấn Enter hoặc Ctrl+C để dừng client)\n");
    Console.ResetColor();

    // Yêu cầu lấy số liệu mới nhất ngay khi kết nối
    try
    {
        await connection.InvokeAsync("RequestLatestStats");
    }
    catch
    {
        // Hub tự động gửi stats khi kết nối
    }

    // Giữ kết nối hoạt động
    await Task.Run(() => Console.ReadLine());
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[LỖI] Không thể kết nối tới SignalR Hub: {ex.Message}");
    Console.WriteLine("Vui lòng đảm bảo EventFlow.DashboardService đang chạy trên cổng 5024.");
    Console.ResetColor();
}
finally
{
    await connection.DisposeAsync();
    Console.WriteLine("[4] Đã đóng kết nối SignalR an toàn.");
}
