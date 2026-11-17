using MediQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.IntegrationTests.Persistence;

public sealed class MediQueueDatabaseReadTests
{
    [SqlServerFact]
    public async Task Applied_schema_supports_read_only_queries_for_all_application_sets()
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MediQueue")!;
        var options = new DbContextOptionsBuilder<MediQueueDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new MediQueueDbContext(options);

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());

        long[] rowCounts =
        [
            await context.Patients.AsNoTracking().LongCountAsync(),
            await context.Doctors.AsNoTracking().LongCountAsync(),
            await context.Departments.AsNoTracking().LongCountAsync(),
            await context.Rooms.AsNoTracking().LongCountAsync(),
            await context.DoctorSchedules.AsNoTracking().LongCountAsync(),
            await context.TimeSlots.AsNoTracking().LongCountAsync(),
            await context.Appointments.AsNoTracking().LongCountAsync(),
            await context.DailyQueues.AsNoTracking().LongCountAsync(),
            await context.QueueTickets.AsNoTracking().LongCountAsync(),
            await context.QueueTransitions.AsNoTracking().LongCountAsync(),
            await context.OutboxMessages.AsNoTracking().LongCountAsync(),
            await context.InboxMessages.AsNoTracking().LongCountAsync(),
            await context.IdempotencyRequests.AsNoTracking().LongCountAsync(),
            await context.AuditLogs.AsNoTracking().LongCountAsync()
        ];

        Assert.All(rowCounts, count => Assert.Equal(0, count));
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__MediQueue")))
        {
            Skip = "Set ConnectionStrings__MediQueue to run SQL Server validation.";
        }
    }
}
