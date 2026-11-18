using Microsoft.AspNetCore.SignalR.Client;

var hubUrl = "http://localhost:5001/hubs/ping";
Console.WriteLine($"Connecting to {hubUrl}");

await using var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();

connection.On<string, string>("ReceiveMessage", (user, message) =>
    Console.WriteLine($"[Broadcast/Group] {user}: {message}"));

connection.On<string>("SystemMessage", message =>
    Console.WriteLine($"[System] {message}"));

connection.Reconnecting += error =>
{
    Console.WriteLine($"Reconnecting: {error?.Message}");
    return Task.CompletedTask;
};
connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Reconnected. New ConnectionId: {connectionId}");
    return Task.CompletedTask;
};
connection.Closed += error =>
{
    Console.WriteLine($"Connection closed: {error?.Message}");
    return Task.CompletedTask;
};

try
{
    await connection.StartAsync();
    Console.WriteLine($"Connected. ConnectionId: {connection.ConnectionId}");

    var echo = await connection.InvokeAsync<string>("Echo", "Xin chao SignalR");
    Console.WriteLine(echo);

    await connection.InvokeAsync("JoinGroup", "demo-room");
    await connection.InvokeAsync("Broadcast", "Ping.Client", "Hello to all connected clients!");
    await connection.InvokeAsync("SendToGroup", "demo-room", "Ping.Client", "Hello demo-room!");

    Console.WriteLine("Demo complete. Press Enter to disconnect.");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"SignalR client error: {ex.Message}");
    Console.WriteLine("Check that Ping.Api is running at http://localhost:5001.");
}
