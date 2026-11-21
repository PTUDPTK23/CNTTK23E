using MediQueue.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.StaffFullName)
            .HasMaxLength(150);

        builder.Property(user => user.IsActive)
            .HasDefaultValue(true);

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnType("datetimeoffset(0)");
    }
}
