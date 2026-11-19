using MediQueue.Appointment.Api.Bootstrap;
using MediQueue.Appointment.Api.Security;
using MediQueue.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediQueueInfrastructure(builder.Configuration);

if (BootstrapCommandRunner.IsBootstrapCommand(args))
{
    using WebApplication bootstrapApplication = builder.Build();
    Environment.ExitCode = await BootstrapCommandRunner.RunAsync(
        bootstrapApplication.Services,
        args);
    return;
}

builder.Services.AddMediQueueAuthentication(builder.Configuration);
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
