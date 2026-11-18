using ClinicQueue.Worker;
using ClinicQueue.Worker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);
// Console logging works in containers and unprivileged Windows terminals.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
var connection = builder.Configuration.GetConnectionString("ClinicQueue")
    ?? throw new InvalidOperationException("Set ConnectionStrings__ClinicQueue using environment variables or user secrets.");
builder.Services.AddDbContext<WorkerDbContext>(o => o.UseSqlServer(connection,
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "worker")));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sql-server");
builder.Services.AddHostedService<Worker>();
using var host = builder.Build();
if (args.Contains("--migrate"))
{
    using var scope = host.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<WorkerDbContext>().Database.MigrateAsync();
    return;
}
await host.RunAsync();
