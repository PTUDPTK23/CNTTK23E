using MediQueue.Appointment.Api.Services.Users;
using MediQueue.Appointment.Api.Services.Catalogs;
using MediQueue.Contracts.Catalogs;
using MediQueue.Contracts.Users;
using MediQueue.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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

        Assert.All(rowCounts, count => Assert.True(count >= 0));
    }

    [SqlServerFact]
    public async Task Catalog_queries_run_read_only_against_applied_schema()
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MediQueue")!;
        var options = new DbContextOptionsBuilder<MediQueueDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new MediQueueDbContext(options);
        var departments = new DepartmentService(context, TimeProvider.System);
        var rooms = new RoomService(context, TimeProvider.System);

        foreach (DepartmentListQuery query in new DepartmentListQuery[] { new(), new(Search: "a", SortBy: "name"), new(Status: "active", SortBy: "status", SortDirection: "desc", PageSize: 20) })
        {
            var result = await departments.GetAsync(query, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error?.Detail);
        }
        foreach (RoomListQuery query in new RoomListQuery[] { new(), new(Search: "p", SortBy: "location"), new(Status: "inactive", SortBy: "status", PageSize: 50) })
        {
            var result = await rooms.GetAsync(query, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error?.Detail);
        }

        Guid? departmentId = await context.Departments.AsNoTracking().Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        if (departmentId.HasValue) Assert.True((await departments.GetAsync(departmentId.Value, CancellationToken.None)).Succeeded);
        Guid? roomId = await context.Rooms.AsNoTracking().Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
        if (roomId.HasValue) Assert.True((await rooms.GetAsync(roomId.Value, CancellationToken.None)).Succeeded);
    }

    [SqlServerFact]
    public async Task User_management_queries_run_read_only_against_applied_schema()
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MediQueue")!;
        var options = new DbContextOptionsBuilder<MediQueueDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new MediQueueDbContext(options);
        var service = new UserManagementService(
            context,
            null!,
            TimeProvider.System,
            NullLogger<UserManagementService>.Instance);

        UserListQuery[] queries =
        [
            new(),
            new() { Search = "@", SortBy = "email", SortDirection = "asc" },
            new() { Role = "Admin", Status = "active", Lockout = "unlocked" },
            new() { SortBy = "name", SortDirection = "desc", Page = 1, PageSize = 20 }
        ];

        foreach (UserListQuery query in queries)
        {
            var result = await service.GetUsersAsync(query, CancellationToken.None);
            Assert.True(result.Succeeded, result.Error?.Detail);
            Assert.NotNull(result.Value);
            Assert.All(result.Value.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DisplayName)));
        }

        int assignedRoleCount = await context.UserRoles.AsNoTracking().CountAsync();
        var defaultResult = await service.GetUsersAsync(new UserListQuery { PageSize = 50 }, CancellationToken.None);
        Assert.True(defaultResult.Succeeded, defaultResult.Error?.Detail);
        Assert.Equal(assignedRoleCount, defaultResult.Value!.Summary.Total);

        Guid? firstUserId = await context.Users
            .AsNoTracking()
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync();
        if (firstUserId.HasValue)
        {
            var details = await service.GetUserAsync(firstUserId.Value, CancellationToken.None);
            Assert.True(details.Succeeded, details.Error?.Detail);
            Assert.False(string.IsNullOrWhiteSpace(details.Value!.DisplayName));
        }

        int usersWithoutStaffName = await context.Users
            .AsNoTracking()
            .CountAsync(user => user.StaffFullName == null);
        if (usersWithoutStaffName > 0)
        {
            Assert.All(defaultResult.Value.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DisplayName)));
        }
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
