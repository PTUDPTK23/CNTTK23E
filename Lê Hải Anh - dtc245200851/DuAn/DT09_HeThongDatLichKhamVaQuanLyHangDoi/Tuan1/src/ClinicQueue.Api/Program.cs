using ClinicQueue.Contracts;
using ClinicQueue.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
// Console logging works in containers and unprivileged Windows terminals.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
var connection = builder.Configuration.GetConnectionString("ClinicQueue")
    ?? throw new InvalidOperationException("Set ConnectionStrings__ClinicQueue using environment variables or user secrets.");
builder.Services.AddDbContext<ApiDbContext>(o => o.UseSqlServer(connection,
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "api")));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sql-server", tags: ["ready"]);
builder.Services.AddOpenApi();
var app = builder.Build();
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApiDbContext>().Database.MigrateAsync();
    return;
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapGet("/api/system", () => new { name = SystemInfo.Name, milestone = "M0", status = "ready" });
app.Run();
