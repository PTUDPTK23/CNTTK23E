var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration.GetValue<int>("PORT", 5000);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");

// API configuration endpoint for frontend to discover backend URLs
app.MapGet("/config", (IConfiguration config) =>
{
    return Results.Ok(new
    {
        apiUrl = config.GetValue<string>("ApiUrl", "http://localhost:5001"),
        hubUrl = config.GetValue<string>("HubUrl", "http://localhost:5003"),
        auditUrl = config.GetValue<string>("AuditUrl", "http://localhost:5004")
    });
});

app.Run();
