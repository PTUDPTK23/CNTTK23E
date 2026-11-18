using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "PingApi", status = "running" }));
app.MapHub<PingHub>("/hubs/ping");
app.Run();

sealed class PingHub : Hub
{
}
