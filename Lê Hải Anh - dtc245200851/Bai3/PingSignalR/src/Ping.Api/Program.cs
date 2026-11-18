var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSignalR()
    .AddJsonProtocol();

var app = builder.Build();

app.MapHub<PingHub>("/ping");

app.Run();

public sealed class PingHub : Microsoft.AspNetCore.SignalR.Hub
{
}