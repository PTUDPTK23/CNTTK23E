using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("IdempotencyRequests", table =>
            table.HasCheckConstraint(
                "CK_IdempotencyRequests_ResponseStatusCode",
                "[ResponseStatusCode] >= 100 AND [ResponseStatusCode] <= 599"));

        builder.HasKey(request => new { request.UserId, request.IdempotencyKey });

        builder.Property(request => request.RequestHash)
            .HasMaxLength(128)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(request => request.CreatedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.HasIndex(request => request.CreatedAtUtc)
            .HasDatabaseName("IX_IdempotencyRequests_CreatedAtUtc");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(request => request.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
