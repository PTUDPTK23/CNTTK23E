
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 1. Khởi tạo kết nối SignalR
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5200/hubs/ping")
    .WithAutomaticReconnect()
    .Build();

// 2. Nhận tin nhắn từ Server (gửi chung và theo nhóm)
connection.On<string>("ReceivePing", message =>
{
    Console.WriteLine($"\n[SERVER PHẢN HỒI]: {message}");
});

// 3. Nhận phản hồi Echo riêng cho máy khách vừa gửi
connection.On<string>("EchoResponse", message =>
{
    Console.WriteLine($"\n[ECHO PHẢN HỒI]: {message}");
});

// 4. Thông báo khi mất kết nối
connection.Reconnecting += error =>
{
    Console.WriteLine("\n[MẤT KẾT NỐI] Đang thử kết nối lại...");
    return Task.CompletedTask;
};

// 5. Thông báo khi kết nối lại thành công
connection.Reconnected += connectionId =>
{
    Console.WriteLine("\n[ĐÃ KẾT NỐI LẠI] SignalR hoạt động bình thường!");
    return Task.CompletedTask;
};

// 6. Thông báo khi kết nối bị đóng hoàn toàn
connection.Closed += error =>
{
    Console.WriteLine("\n[KẾT NỐI ĐÃ ĐÓNG] Không thể kết nối lại.");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();

    Console.WriteLine("Đã kết nối SignalR thành công!");
    Console.WriteLine("Các lệnh sử dụng:");
    Console.WriteLine("/echo noi-dung              - Gửi tin nhắn Echo");
    Console.WriteLine("/join ten-nhom              - Tham gia nhóm");
    Console.WriteLine("/leave ten-nhom             - Rời nhóm");
    Console.WriteLine("/group ten-nhom noi-dung    - Gửi tin nhắn trong nhóm");
    Console.WriteLine("/all noi-dung               - Gửi tin nhắn đến tất cả");
    Console.WriteLine("exit                        - Thoát");

    while (true)
    {
        Console.Write("\nBạn: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            continue;

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        var parts = input.Split(
            ' ',
            3,
            StringSplitOptions.RemoveEmptyEntries
        );

        try
        {
            if (parts.Length >= 2 && parts[0] == "/echo")
            {
                await connection.InvokeAsync(
                    "Echo", input.Substring(6)
                );
            }
            else if (parts.Length == 2 && parts[0] == "/join")
            {
                await connection.InvokeAsync(
                    "JoinGroup", parts[1]
                );
            }
            else if (parts.Length == 2 && parts[0] == "/leave")
            {
                await connection.InvokeAsync(
                    "LeaveGroup", parts[1]
                );
            }
            else if (parts.Length == 3 && parts[0] == "/group")
            {
                await connection.InvokeAsync(
                    "SendToGroup", parts[1], parts[2]
                );
            }
            else if (parts.Length >= 2 && parts[0] == "/all")
            {
                await connection.InvokeAsync(
                    "SendPing", input.Substring(5)
                );
            }
            else
            {
                Console.WriteLine(
                    "Lệnh không hợp lệ. Hãy kiểm tra hướng dẫn."
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Gửi tin nhắn thất bại: {ex.Message}"
            );

            Console.WriteLine(
                "Hãy chờ kết nối được khôi phục rồi thử lại."
            );
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Lỗi kết nối: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}
