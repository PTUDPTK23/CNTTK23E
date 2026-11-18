var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "PingApi scaffold");
app.Run();
