using MediQueue.Domain.Doctors;
using MediQueue.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(doctor => doctor.Id);

        builder.Property(doctor => doctor.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(doctor => doctor.RowVersion)
            .IsRowVersion();

        builder.HasIndex(doctor => doctor.UserId)
            .IsUnique()
            .HasDatabaseName("UX_Doctors_UserId");

        builder.HasIndex(doctor => new { doctor.DepartmentId, doctor.IsActive })
            .HasDatabaseName("IX_Doctors_DepartmentId_IsActive");

        builder.HasOne(doctor => doctor.Department)
            .WithMany(department => department.Doctors)
            .HasForeignKey(doctor => doctor.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Doctor>(doctor => doctor.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
