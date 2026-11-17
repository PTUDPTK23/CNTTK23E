using MediQueue.Infrastructure;
using MediQueue.Queue.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMediQueueInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
