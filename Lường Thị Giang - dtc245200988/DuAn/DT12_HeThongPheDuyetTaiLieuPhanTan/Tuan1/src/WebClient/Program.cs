using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Cung cấp cấu hình môi trường động cho Frontend mà không hardcode trong JS
app.MapGet("/config.js", (IConfiguration config) =>
{
    var payload = new
    {
        documentApiUrl = config["PUBLIC_DOCUMENT_API_URL"] ?? "http://localhost:5001",
        notificationUrl = config["PUBLIC_NOTIFICATION_URL"] ?? "http://localhost:5002"
    };
    var json = JsonSerializer.Serialize(payload);
    return Results.Text($"window.APP_CONFIG = {json};", "application/javascript");
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "WebClient",
    timestampUtc = DateTime.UtcNow
}));

app.Run();
