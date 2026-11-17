using MediQueue.Domain.Appointments;
using MediQueue.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", table =>
        {
            table.HasCheckConstraint(
                "CK_Appointments_Status",
                "[Status] IN (N'PendingNumber', N'Waiting', N'Serving', N'Completed', N'Cancelled', N'NoShow')");
        });

        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(appointment => appointment.CreatedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(appointment => appointment.UpdatedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(appointment => appointment.CheckedInAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(appointment => appointment.CancelledAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(appointment => appointment.NoShowAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(appointment => appointment.CancellationReason)
            .HasMaxLength(500);

        builder.Property(appointment => appointment.RowVersion)
            .IsRowVersion();

        builder.HasIndex(appointment => new
        {
            appointment.PatientId,
            appointment.Status,
            appointment.TimeSlotId
        })
            .HasDatabaseName("IX_Appointments_Patient_Status_TimeSlot");

        builder.HasIndex(appointment => new { appointment.TimeSlotId, appointment.Status })
            .HasDatabaseName("IX_Appointments_TimeSlot_Status");

        builder.HasOne(appointment => appointment.Patient)
            .WithMany(patient => patient.Appointments)
            .HasForeignKey(appointment => appointment.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.TimeSlot)
            .WithMany(slot => slot.Appointments)
            .HasForeignKey(appointment => appointment.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(appointment => appointment.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(appointment => appointment.CheckedInByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
