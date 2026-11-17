using MediQueue.Domain.Appointments;
using MediQueue.Domain.Queues;
using MediQueue.Domain.Scheduling;
using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using DomainAppointment = MediQueue.Domain.Appointments.Appointment;

namespace MediQueue.IntegrationTests.Persistence;

public sealed class MediQueueModelTests
{
    private readonly IModel model = CreateContext().Model;

    [Fact]
    public void Model_contains_business_identity_and_messaging_tables()
    {
        string?[] expectedTables =
        [
            "AspNetUsers",
            "Patients",
            "Doctors",
            "Departments",
            "Rooms",
            "DoctorSchedules",
            "TimeSlots",
            "Appointments",
            "DailyQueues",
            "QueueTickets",
            "QueueTransitions",
            "OutboxMessages",
            "InboxMessages",
            "IdempotencyRequests",
            "AuditLogs"
        ];

        string?[] actualTables = model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Distinct()
            .ToArray();

        Assert.All(expectedTables, table => Assert.Contains(table, actualTables));
    }

    [Fact]
    public void Queue_ticket_has_required_unique_and_filtered_indexes()
    {
        IEntityType entity = GetEntity<QueueTicket>();

        IIndex appointmentIndex = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(["AppointmentId"]));
        IIndex numberIndex = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(["DoctorId", "ServiceDate", "QueueNumber"]));
        IIndex servingIndex = entity.GetIndexes().Single(index =>
            index.GetDatabaseName() == "UX_QueueTickets_Doctor_Serving");

        Assert.True(appointmentIndex.IsUnique);
        Assert.True(numberIndex.IsUnique);
        Assert.True(servingIndex.IsUnique);
        Assert.Equal("[Status] = N'Serving'", servingIndex.GetFilter());
    }

    [Fact]
    public void Capacity_status_and_queue_number_checks_are_mapped()
    {
        using MediQueueDbContext context = CreateContext();
        IModel designTimeModel = context.GetService<IDesignTimeModel>().Model;

        string[] checkConstraintNames = designTimeModel.GetEntityTypes()
            .SelectMany(entity => entity.GetCheckConstraints())
            .Select(constraint => constraint.Name)
            .OfType<string>()
            .ToArray();

        Assert.Contains("CK_TimeSlots_Capacity", checkConstraintNames);
        Assert.Contains("CK_TimeSlots_BookedCount", checkConstraintNames);
        Assert.Contains("CK_TimeSlots_TimeRange", checkConstraintNames);
        Assert.Contains("CK_Appointments_Status", checkConstraintNames);
        Assert.Contains("CK_QueueTickets_QueueNumber", checkConstraintNames);
        Assert.Contains("CK_QueueTickets_Status", checkConstraintNames);
    }

    [Fact]
    public void Concurrent_aggregates_use_rowversion()
    {
        Type[] concurrentTypes =
        [
            typeof(TimeSlot),
            typeof(DomainAppointment),
            typeof(DailyQueue),
            typeof(QueueTicket)
        ];

        foreach (Type type in concurrentTypes)
        {
            IProperty rowVersion = model.FindEntityType(type)!.FindProperty("RowVersion")!;

            Assert.True(rowVersion.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
        }
    }

    [Fact]
    public void Domain_relationships_do_not_cascade_delete_history()
    {
        IEnumerable<IForeignKey> domainForeignKeys = model.GetEntityTypes()
            .Where(entity => entity.ClrType.Namespace?.StartsWith("MediQueue.Domain", StringComparison.Ordinal) == true)
            .SelectMany(entity => entity.GetForeignKeys());

        Assert.DoesNotContain(domainForeignKeys, foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
    }

    [Fact]
    public void Queue_ticket_foreign_key_keeps_doctor_and_service_date_aligned_with_daily_queue()
    {
        IEntityType ticket = GetEntity<QueueTicket>();
        IForeignKey dailyQueueForeignKey = ticket.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(DailyQueue));

        Assert.Equal(
            ["DailyQueueId", "DoctorId", "ServiceDate"],
            dailyQueueForeignKey.Properties.Select(property => property.Name));
        Assert.Equal(
            ["Id", "DoctorId", "ServiceDate"],
            dailyQueueForeignKey.PrincipalKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Restrict, dailyQueueForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Identity_uses_guid_key_and_no_duplicate_user_table()
    {
        IEntityType user = GetEntity<ApplicationUser>();

        Assert.Equal("AspNetUsers", user.GetTableName());
        Assert.Equal(typeof(Guid), user.FindPrimaryKey()!.Properties.Single().ClrType);
    }

    [Fact]
    public void Messaging_deduplication_keys_and_pending_outbox_index_are_mapped()
    {
        IEntityType inbox = GetEntity<InboxMessage>();
        IEntityType outbox = GetEntity<OutboxMessage>();

        Assert.Equal(
            ["MessageId", "ConsumerName"],
            inbox.FindPrimaryKey()!.Properties.Select(property => property.Name));

        IIndex pendingIndex = outbox.GetIndexes().Single(index =>
            index.GetDatabaseName() == "IX_OutboxMessages_Pending");
        Assert.Equal("[PublishedAtUtc] IS NULL", pendingIndex.GetFilter());
    }

    private IEntityType GetEntity<TEntity>() =>
        model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"Entity {typeof(TEntity).Name} is not mapped.");

    private static MediQueueDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MediQueueDbContext>()
            .UseSqlServer(
                "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new MediQueueDbContext(options);
    }
}
