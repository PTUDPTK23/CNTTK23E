using Notification.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHostedService<RabbitMqNotificationConsumer>();

var app = builder.Build();

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
