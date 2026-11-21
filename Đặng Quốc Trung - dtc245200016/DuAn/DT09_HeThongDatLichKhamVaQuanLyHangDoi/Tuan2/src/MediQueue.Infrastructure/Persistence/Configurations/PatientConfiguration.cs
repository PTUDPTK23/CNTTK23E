using MediQueue.Domain.Patients;
using MediQueue.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(patient => patient.PhoneNumber)
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.Property(patient => patient.DateOfBirth)
            .HasColumnType("date");

        builder.Property(patient => patient.RowVersion)
            .IsRowVersion();

        builder.HasIndex(patient => patient.UserId)
            .IsUnique()
            .HasDatabaseName("UX_Patients_UserId");

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Patient>(patient => patient.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
