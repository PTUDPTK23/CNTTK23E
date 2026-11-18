using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5200";
var name = args.Length > 1 ? args[1] : "Client";
string Option(string key, string fallback)
{
    var i = Array.IndexOf(args, key);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}
// Tham số thí nghiệm thay cho việc sửa sai rồi quên phục hồi mã nguồn.
var hubPath = Option("--hub-path", "/hubs/ping");
var echoEvent = Option("--event", "EchoResponse");
var echoMethod = Option("--method", "Echo");
await using var connection = new HubConnectionBuilder()
    .WithUrl($"{serverUrl}{hubPath}")
    .WithAutomaticReconnect().Build();
connection.On<string>(echoEvent, msg => Console.WriteLine($"[Echo] {msg}"));
connection.On<string>("BroadcastMessage", msg => Console.WriteLine($"[Broadcast] {msg}"));
connection.On<string>("GroupMessage", msg => Console.WriteLine($"[Group] {msg}"));
connection.On<string>("SystemMessage", msg => Console.WriteLine($"[System] {msg}"));
connection.Reconnecting += _ =>
{
    Console.WriteLine("[Lifecycle] Dang ket noi lai...");
    return Task.CompletedTask;
};
connection.Reconnected += async id =>
{
    Console.WriteLine($"[Lifecycle] Da ket noi lai: {id}");
    try
    {
        await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
        Console.WriteLine("[Lifecycle] Da vao lai nhom lop-hoc-01.");
    }
    catch (Exception ex) { Console.WriteLine($"[Lifecycle] Khong vao lai duoc nhom: {ex.Message}"); }
};
connection.Closed += _ =>
{
    Console.WriteLine("[Lifecycle] Ket noi da dong.");
    return Task.CompletedTask;
};
try
{
    await connection.StartAsync();
    Console.WriteLine($"{name} da ket noi den PingApi. ConnectionId: {connection.ConnectionId}");
    await connection.InvokeAsync(echoMethod, $"Xin chao SignalR tu {name}");
    await connection.InvokeAsync("JoinGroup", "lop-hoc-01");
    await connection.InvokeAsync("SendToGroup", "lop-hoc-01", $"Thong bao nhom tu {name}");
    await connection.InvokeAsync("Broadcast", $"Xin chao tat ca tu {name}");
    Console.WriteLine("Dang cho thong diep... Nhan Enter de thoat.");
    if (int.TryParse(Option("--wait-seconds", ""), out var seconds))
        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(seconds, 0, 120)));
    else
        Console.ReadLine();
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Loi ket noi: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (Microsoft.AspNetCore.SignalR.HubException ex)
{
    Console.WriteLine($"HubException: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
{
    Console.WriteLine($"Loi: {ex.Message}");
    Environment.ExitCode = 1;
}
