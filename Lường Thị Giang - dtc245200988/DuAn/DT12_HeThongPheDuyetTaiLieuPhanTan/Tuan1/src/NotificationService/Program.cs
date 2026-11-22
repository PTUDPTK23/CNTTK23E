using NotificationService.Hubs;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddHostedService<RabbitMqConsumerWorker>();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", (NotificationStore store) => Results.Ok(new
{
    status = "Healthy",
    service = "NotificationService",
    processedEvents = store.ProcessedCount,
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/notifications", (NotificationStore store) => Results.Ok(store.GetRecent(50)));

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
