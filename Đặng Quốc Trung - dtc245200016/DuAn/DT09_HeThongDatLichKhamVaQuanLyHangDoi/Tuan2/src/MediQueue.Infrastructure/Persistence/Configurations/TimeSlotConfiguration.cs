using MediQueue.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("TimeSlots", table =>
        {
            table.HasCheckConstraint(
                "CK_TimeSlots_TimeRange",
                "[EndAtUtc] > [StartAtUtc]");
            table.HasCheckConstraint(
                "CK_TimeSlots_Capacity",
                "[Capacity] > 0");
            table.HasCheckConstraint(
                "CK_TimeSlots_BookedCount",
                "[BookedCount] >= 0 AND [BookedCount] <= [Capacity]");
        });

        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.StartAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(slot => slot.EndAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(slot => slot.RowVersion)
            .IsRowVersion();

        builder.HasIndex(slot => new { slot.DoctorId, slot.StartAtUtc, slot.IsActive })
            .HasDatabaseName("IX_TimeSlots_Doctor_Start_IsActive");

        builder.HasIndex(slot => new { slot.RoomId, slot.StartAtUtc, slot.EndAtUtc })
            .HasDatabaseName("IX_TimeSlots_Room_TimeRange");

        builder.HasOne(slot => slot.DoctorSchedule)
            .WithMany(schedule => schedule.TimeSlots)
            .HasForeignKey(slot => slot.DoctorScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(slot => slot.Doctor)
            .WithMany(doctor => doctor.TimeSlots)
            .HasForeignKey(slot => slot.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(slot => slot.Room)
            .WithMany(room => room.TimeSlots)
            .HasForeignKey(slot => slot.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
