using MediQueue.Domain.Queues;
using MediQueue.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class QueueTransitionConfiguration : IEntityTypeConfiguration<QueueTransition>
{
    public void Configure(EntityTypeBuilder<QueueTransition> builder)
    {
        builder.ToTable("QueueTransitions");
        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(transition => transition.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(transition => transition.OccurredAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(transition => transition.Reason)
            .HasMaxLength(500);

        builder.HasIndex(transition => new { transition.QueueTicketId, transition.OccurredAtUtc })
            .HasDatabaseName("IX_QueueTransitions_Ticket_OccurredAt");

        builder.HasOne(transition => transition.QueueTicket)
            .WithMany(ticket => ticket.Transitions)
            .HasForeignKey(transition => transition.QueueTicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(transition => transition.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
