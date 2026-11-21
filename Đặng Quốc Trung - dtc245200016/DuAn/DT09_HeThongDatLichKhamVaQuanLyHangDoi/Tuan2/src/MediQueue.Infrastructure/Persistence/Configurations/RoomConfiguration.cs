using MediQueue.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(room => room.Id);

        builder.Property(room => room.Code)
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(room => room.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(room => room.Location)
            .HasMaxLength(300);

        builder.HasIndex(room => room.Code)
            .IsUnique()
            .HasDatabaseName("UX_Rooms_Code");
    }
}
