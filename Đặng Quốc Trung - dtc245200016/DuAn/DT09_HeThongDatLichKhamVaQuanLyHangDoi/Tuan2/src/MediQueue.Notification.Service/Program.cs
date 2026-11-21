using MediQueue.Contracts.Health;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok(new ServiceStatusResponse("MediQueue.Notification.Service", "Healthy")));

app.Run();
