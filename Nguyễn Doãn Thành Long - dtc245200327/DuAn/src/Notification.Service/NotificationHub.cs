using Microsoft.AspNetCore.SignalR;

namespace Notification.Service;

public sealed class NotificationHub : Hub
{
    public async Task JoinStudentGroup(string studentId)
    {
        var groupName = $"student:{studentId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[Notification.Service] Client {Context.ConnectionId[..8]} đã đăng ký nhận thông báo cho sinh viên '{studentId}'.");
    }
}
