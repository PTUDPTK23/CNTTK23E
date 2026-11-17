using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Audit;
using Notification.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AuditSubscriber>();
var host = builder.Build();
await host.Services.GetRequiredService<NotificationStore>().InitializeAsync(CancellationToken.None);
await host.RunAsync();
