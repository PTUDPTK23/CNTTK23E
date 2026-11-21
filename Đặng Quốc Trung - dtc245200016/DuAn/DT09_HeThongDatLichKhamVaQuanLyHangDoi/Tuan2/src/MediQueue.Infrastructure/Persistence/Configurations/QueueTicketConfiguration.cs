using MediQueue.Domain.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class QueueTicketConfiguration : IEntityTypeConfiguration<QueueTicket>
{
    public void Configure(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("QueueTickets", table =>
        {
            table.HasCheckConstraint(
                "CK_QueueTickets_QueueNumber",
                "[QueueNumber] > 0");
            table.HasCheckConstraint(
                "CK_QueueTickets_Status",
                "[Status] IN (N'Waiting', N'Serving', N'Completed', N'Cancelled', N'NoShow')");
        });

        builder.HasKey(ticket => ticket.Id);
        builder.Ignore(ticket => ticket.DisplayNumber);

        builder.Property(ticket => ticket.ServiceDate)
            .HasColumnType("date");

        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(ticket => ticket.IssuedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(ticket => ticket.ServingAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(ticket => ticket.CompletedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(ticket => ticket.CancelledAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(ticket => ticket.NoShowAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(ticket => ticket.RowVersion)
            .IsRowVersion();

        builder.HasIndex(ticket => ticket.AppointmentId)
            .IsUnique()
            .HasDatabaseName("UX_QueueTickets_AppointmentId");

        builder.HasIndex(ticket => new { ticket.DoctorId, ticket.ServiceDate, ticket.QueueNumber })
            .IsUnique()
            .HasDatabaseName("UX_QueueTickets_Doctor_ServiceDate_Number");

        builder.HasIndex(ticket => ticket.DoctorId)
            .IsUnique()
            .HasFilter("[Status] = 'Serving'")
            .HasDatabaseName("UX_QueueTickets_Doctor_Serving");

        builder.HasIndex(ticket => new
        {
            ticket.DoctorId,
            ticket.ServiceDate,
            ticket.Status,
            ticket.QueueNumber
        })
            .HasDatabaseName("IX_QueueTickets_Doctor_Date_Status_Number");

        builder.HasOne(ticket => ticket.DailyQueue)
            .WithMany(queue => queue.Tickets)
            .HasForeignKey(ticket => new { ticket.DailyQueueId, ticket.DoctorId, ticket.ServiceDate })
            .HasPrincipalKey(queue => new { queue.Id, queue.DoctorId, queue.ServiceDate })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ticket => ticket.Doctor)
            .WithMany(doctor => doctor.QueueTickets)
            .HasForeignKey(ticket => ticket.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ticket => ticket.Appointment)
            .WithOne(appointment => appointment.QueueTicket)
            .HasForeignKey<QueueTicket>(ticket => ticket.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
