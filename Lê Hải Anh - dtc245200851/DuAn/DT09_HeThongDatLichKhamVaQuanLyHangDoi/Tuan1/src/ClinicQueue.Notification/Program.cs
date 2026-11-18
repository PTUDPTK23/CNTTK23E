using Microsoft.AspNetCore.Diagnostics.HealthChecks;
var builder = WebApplication.CreateBuilder(args);
// Console logging works in containers and unprivileged Windows terminals.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Services.AddHealthChecks();
var app = builder.Build();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
// SignalR and consumers belong to M4.
app.Run();
