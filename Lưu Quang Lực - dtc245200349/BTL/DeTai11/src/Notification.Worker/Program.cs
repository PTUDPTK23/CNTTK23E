using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Shared;
using Notification.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddHostedService<DeliveryWorker>();
var host = builder.Build();
await host.Services.GetRequiredService<NotificationStore>().InitializeAsync(CancellationToken.None);
await host.RunAsync();
