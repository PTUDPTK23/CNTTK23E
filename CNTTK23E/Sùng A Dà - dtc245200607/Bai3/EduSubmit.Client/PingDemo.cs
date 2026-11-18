using Microsoft.AspNetCore.SignalR.Client;

public static class PingDemo
{
    public static async Task<int> RunAsync(string[] args)
    {
        var apiBaseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
        var name = args.Length > 1 ? args[1] : "Client";
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var baseUri))
        {
            Console.Error.WriteLine($"URL API không hợp lệ: {apiBaseUrl}");
            return 1;
        }

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(baseUri, "/hubs/ping"))
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("EchoResponse", message => Console.WriteLine($"[Echo] {message}"));
        connection.On<string>("BroadcastMessage", message => Console.WriteLine($"[Broadcast] {message}"));
        connection.On<string>("GroupMessage", message => Console.WriteLine($"[Group] {message}"));
        connection.On<string>("SystemMessage", message => Console.WriteLine($"[System] {message}"));
        connection.Reconnecting += error =>
        {
            Console.WriteLine($"[Lifecycle] Đang kết nối lại: {error?.Message}");
            return Task.CompletedTask;
        };
        connection.Reconnected += connectionId =>
        {
            Console.WriteLine($"[Lifecycle] Đã kết nối lại: {connectionId}");
            return Task.CompletedTask;
        };
        connection.Closed += error =>
        {
            Console.WriteLine($"[Lifecycle] Kết nối đã đóng: {error?.Message}");
            return Task.CompletedTask;
        };

        try
        {
            await connection.StartAsync();
            Console.WriteLine($"{name} đã kết nối đến PingHub. ConnectionId: {connection.ConnectionId}");
            await connection.InvokeAsync("Echo", $"Xin chào SignalR từ {name}");
            await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
            await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thông báo nhóm từ {name}");
            await connection.InvokeAsync("Broadcast", $"Xin chào tất cả từ {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Không thể chạy PingHub demo: {ex.Message}");
            return 1;
        }

        Console.WriteLine("Đang chờ thông điệp... Nhấn Enter để thoát.");
        if (args.Length > 2 && int.TryParse(args[2], out var waitSeconds) && waitSeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
        }
        else
        {
            Console.ReadLine();
        }
        return 0;
    }
}
