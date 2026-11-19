using MediQueue.Domain.Security;
using MediQueue.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Infrastructure.Identity;

public sealed class IdentityBootstrapper(
    MediQueueDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager) : IIdentityBootstrapper
{
    private const string ApprovedDatabaseName = "MediQueue";
    private const string InitialMigrationId = "20261117142359_InitialCreate";

    public async Task<IdentityBootstrapResult> InitializeRolesAsync(
        CancellationToken cancellationToken = default)
    {
        IdentityBootstrapResult? guardFailure = await ValidateDatabaseAsync(cancellationToken);
        if (guardFailure is not null)
        {
            return guardFailure;
        }

        var createdRoles = new List<string>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (string roleName in SystemRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            IdentityResult result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                string codes = string.Join(", ", result.Errors.Select(error => error.Code));
                return IdentityBootstrapResult.Failure(
                    $"Không thể khởi tạo Role '{roleName}'. Identity error codes: {codes}.");
            }

            createdRoles.Add(roleName);
        }

        await transaction.CommitAsync(cancellationToken);

        return createdRoles.Count == 0
            ? IdentityBootstrapResult.Success(false, "Bốn Role hệ thống đã tồn tại; không có thay đổi.")
            : IdentityBootstrapResult.Success(
                true,
                $"Đã tạo {createdRoles.Count} Role hệ thống còn thiếu.",
                createdRoles);
    }

    public async Task<IdentityBootstrapResult> CreateInitialAdminAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        IdentityBootstrapResult? guardFailure = await ValidateDatabaseAsync(cancellationToken);
        if (guardFailure is not null)
        {
            return guardFailure;
        }

        if (!await roleManager.RoleExistsAsync(SystemRoles.Admin))
        {
            return IdentityBootstrapResult.Failure(
                "Role Admin chưa tồn tại. Hãy chạy bootstrap Role trước.");
        }

        IList<ApplicationUser> existingAdmins = await userManager.GetUsersInRoleAsync(SystemRoles.Admin);
        if (existingAdmins.Count > 0)
        {
            return IdentityBootstrapResult.Success(
                false,
                "Đã có tài khoản Admin; không tạo thêm và không thay đổi mật khẩu.");
        }

        string normalizedEmail = email.Trim();
        ApplicationUser? existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return IdentityBootstrapResult.Failure(
                "Email đã thuộc một tài khoản hiện có; bootstrap không tự nâng quyền tài khoản này.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityBootstrapResult.Failure(FormatIdentityErrors(createResult));
        }

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, SystemRoles.Admin);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return IdentityBootstrapResult.Failure(FormatIdentityErrors(roleResult));
        }

        await transaction.CommitAsync(cancellationToken);
        return IdentityBootstrapResult.Success(true, "Đã tạo tài khoản Admin đầu tiên.");
    }

    private async Task<IdentityBootstrapResult?> ValidateDatabaseAsync(
        CancellationToken cancellationToken)
    {
        string databaseName = dbContext.Database.GetDbConnection().Database;
        if (!string.Equals(databaseName, ApprovedDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityBootstrapResult.Failure(
                $"Bootstrap bị từ chối vì database đích không phải '{ApprovedDatabaseName}'.");
        }

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return IdentityBootstrapResult.Failure("Không thể kết nối database MediQueue.");
        }

        IReadOnlyList<string> appliedMigrations =
            (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        if (!appliedMigrations.Contains(InitialMigrationId, StringComparer.Ordinal))
        {
            return IdentityBootstrapResult.Failure(
                "Database MediQueue chưa có InitialCreate đã được xác minh.");
        }

        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            return IdentityBootstrapResult.Failure(
                "Database MediQueue còn pending Migration; bootstrap bị từ chối.");
        }

        return null;
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        string codes = string.Join(", ", result.Errors.Select(error => error.Code));
        return $"Identity không thể hoàn thành bootstrap. Error codes: {codes}.";
    }
}
