using MediQueue.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediQueueInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.MapControllers();

app.Run();

public partial class Program
{
}
