using Clinic.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder=Host.CreateApplicationBuilder(args);
builder.Services.AddClinic(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<NumberConsumer>();
var host=builder.Build();
await host.Services.InitializeClinic();
await host.RunAsync();

sealed class NumberConsumer(Database db,IConfiguration config,ILogger<NumberConsumer> log) : RabbitConsumer(config,log,"clinic.numbers")
{
    protected override Task Handle(BusMessage message,CancellationToken ct)=>db.AssignNumber(message.AppointmentId);
}
