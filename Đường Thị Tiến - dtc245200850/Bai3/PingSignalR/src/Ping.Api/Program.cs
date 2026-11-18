using Microsoft.AspNetCore.SignalR;
var builder = WebApplication.CreateBuilder(args);
// Việc 1: đăng ký dịch vụ SignalR vào DI
builder.Services.AddSignalR();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(
new { service = "PingApi", status = "running" }));
// Việc 2: ánh xạ hub vào tuyến /hubs/ping
app.MapHub<PingHub>("/hubs/ping");
app.Run();
sealed class PingHub : Hub
{
    public async Task Echo(string message)
{
await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");

}   // bước 3 thêm phương thức
}