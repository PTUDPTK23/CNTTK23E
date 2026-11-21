using Microsoft.AspNetCore.Identity;

namespace MediQueue.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? StaffFullName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
