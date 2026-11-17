using FileProcessing.Infrastructure;
using FileProcessing.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add infrastructure services (DbContext, Storage, Processor, Messaging, OutboxPublisher)
builder.Services.AddInfrastructure(builder.Configuration);

// Add ImageProcessingWorker background service so API can run complete flow standalone
builder.Services.AddHostedService<ImageProcessingWorker>();

builder.Services.AddControllers();

// Configure CORS for local UI and web clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Serve static UI files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
