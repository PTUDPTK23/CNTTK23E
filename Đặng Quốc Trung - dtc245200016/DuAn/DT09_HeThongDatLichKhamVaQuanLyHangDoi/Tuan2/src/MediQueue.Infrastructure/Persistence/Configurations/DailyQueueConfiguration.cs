using MediQueue.Domain.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class DailyQueueConfiguration : IEntityTypeConfiguration<DailyQueue>
{
    public void Configure(EntityTypeBuilder<DailyQueue> builder)
    {
        builder.ToTable("DailyQueues", table =>
        {
            table.HasCheckConstraint(
                "CK_DailyQueues_LastIssuedNumber",
                "[LastIssuedNumber] >= 0");
            table.HasCheckConstraint(
                "CK_DailyQueues_Version",
                "[Version] >= 0");
        });

        builder.HasKey(queue => queue.Id);
        builder.HasAlternateKey(queue => new { queue.Id, queue.DoctorId, queue.ServiceDate });

        builder.Property(queue => queue.ServiceDate)
            .HasColumnType("date");

        builder.Property(queue => queue.RowVersion)
            .IsRowVersion();

        builder.HasIndex(queue => new { queue.DoctorId, queue.ServiceDate })
            .IsUnique()
            .HasDatabaseName("UX_DailyQueues_Doctor_ServiceDate");

        builder.HasOne(queue => queue.Doctor)
            .WithMany(doctor => doctor.DailyQueues)
            .HasForeignKey(queue => queue.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(queue => queue.CurrentTicket)
            .WithMany()
            .HasForeignKey(queue => queue.CurrentTicketId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
