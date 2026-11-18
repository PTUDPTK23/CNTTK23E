
using Microsoft.AspNetCore.SignalR.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5200/pingHub")
    .WithAutomaticReconnect()
    .Build();

// Nhận thông báo từ Server
connection.On<string>("ReceivePing", message =>
{
    Console.WriteLine($"[SERVER PHẢN HỒI]: {message}");
});

try
{
    await connection.StartAsync();

    Console.WriteLine("Đã kết nối SignalR thành công!");
    Console.WriteLine("Nhập nội dung để gửi đến Server.");
    Console.WriteLine("Nhập exit để thoát.");

    while (true)
    {
        Console.Write("\nBạn: ");
        var message = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(message))
            continue;

        if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
            break;

        await connection.InvokeAsync("SendPing", message);
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
