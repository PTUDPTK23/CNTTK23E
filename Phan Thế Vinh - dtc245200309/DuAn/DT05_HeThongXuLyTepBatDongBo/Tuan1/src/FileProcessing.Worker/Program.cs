using FileProcessing.Infrastructure;
using FileProcessing.Infrastructure.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Register Infrastructure services (DbContext, SQL Server, Storage, Processor, Messaging, Outbox)
builder.Services.AddInfrastructure(builder.Configuration);

// Register ImageProcessingWorker
builder.Services.AddHostedService<ImageProcessingWorker>();

var host = builder.Build();
host.Run();
