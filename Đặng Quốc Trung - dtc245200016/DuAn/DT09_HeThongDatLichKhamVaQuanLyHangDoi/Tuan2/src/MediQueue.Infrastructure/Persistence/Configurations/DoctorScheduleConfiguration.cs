using MediQueue.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", table =>
        {
            table.HasCheckConstraint(
                "CK_DoctorSchedules_TimeRange",
                "[EndTime] > [StartTime]");
            table.HasCheckConstraint(
                "CK_DoctorSchedules_DayOfWeek",
                "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
        });

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.StartTime)
            .HasColumnType("time(0)");

        builder.Property(schedule => schedule.EndTime)
            .HasColumnType("time(0)");

        builder.HasIndex(schedule => new
        {
            schedule.DoctorId,
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime
        })
            .HasDatabaseName("IX_DoctorSchedules_Doctor_Day_Time");

        builder.HasIndex(schedule => new
        {
            schedule.RoomId,
            schedule.DayOfWeek,
            schedule.StartTime,
            schedule.EndTime
        })
            .HasDatabaseName("IX_DoctorSchedules_Room_Day_Time");

        builder.HasOne(schedule => schedule.Doctor)
            .WithMany(doctor => doctor.Schedules)
            .HasForeignKey(schedule => schedule.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(schedule => schedule.Room)
            .WithMany(room => room.DoctorSchedules)
            .HasForeignKey(schedule => schedule.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
