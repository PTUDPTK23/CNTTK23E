using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediQueue.Contracts.Users;
using MediQueue.Domain.Common;
using MediQueue.Domain.Security;
using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Users;

public sealed class UserManagementService(
    MediQueueDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    public async Task<AccountServiceResult<PagedUsersResponse>> GetUsersAsync(
        UserListQuery query,
        CancellationToken cancellationToken)
    {
        AccountServiceError? validation = ValidateQuery(query);
        if (validation is not null) return AccountServiceResult<PagedUsersResponse>.Failure(validation);

        DateTimeOffset now = timeProvider.GetUtcNow();
        IQueryable<UserRow> rows = BuildRows(now);
        string? search = NormalizeOptional(query.Search);
        if (search is not null)
        {
            rows = rows.Where(item =>
                item.DisplayName.Contains(search) || item.Email.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(query.Role)) rows = rows.Where(item => item.Role == query.Role);
        rows = query.Status?.ToLowerInvariant() switch
        {
            "active" => rows.Where(item => item.IsActive),
            "inactive" => rows.Where(item => !item.IsActive),
            _ => rows
        };
        rows = query.Lockout?.ToLowerInvariant() switch
        {
            "locked" => rows.Where(item => item.IsLockedOut),
            "unlocked" => rows.Where(item => !item.IsLockedOut),
            _ => rows
        };

        int totalCount = await rows.CountAsync(cancellationToken);
        rows = ApplySort(rows, query.SortBy, query.SortDirection == "desc");
        UserListItemResponse[] items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new UserListItemResponse(
                item.Id, item.DisplayName, item.Email, item.Role,
                item.IsActive, item.IsLockedOut, item.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        UserSummaryResponse summary = await BuildSummaryAsync(now, cancellationToken);
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return AccountServiceResult<PagedUsersResponse>.Success(new PagedUsersResponse(
            items, query.Page, query.PageSize, totalCount, totalPages, summary));
    }

    public async Task<AccountServiceResult<UserDetailsResponse>> GetUserAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        UserDetailsRow? row = await BuildDetailsQuery(timeProvider.GetUtcNow())
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return row is null
            ? Failure<UserDetailsResponse>(404, "Không tìm thấy tài khoản", "Tài khoản không tồn tại.")
            : AccountServiceResult<UserDetailsResponse>.Success(new UserDetailsResponse(
                row.Id,
                row.DisplayName,
                row.Email,
                row.PhoneNumber,
                row.Role,
                row.IsActive,
                row.IsLockedOut,
                row.LockoutEndUtc,
                row.CreatedAtUtc,
                row.HasDomainProfile));
    }

    public async Task<AccountServiceResult<UserDetailsResponse>> CreateStaffAsync(
        ClaimsPrincipal actor,
        CreateStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserManagementValues.StaffRoles.Contains(request.Role, StringComparer.Ordinal))
            return Validation<UserDetailsResponse>(nameof(request.Role), "Chỉ được tạo tài khoản Bác sĩ hoặc Lễ tân.");

        Guid? actorId = GetActorId(actor);
        if (actorId is null) return Failure<UserDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        if (!await dbContext.Roles.AnyAsync(role => role.Name == request.Role, cancellationToken))
            return Failure<UserDetailsResponse>(409, "Role chưa sẵn sàng", "Role nhân sự chưa được khởi tạo.");

        string email = request.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            StaffFullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            IdentityResult created = await userManager.CreateAsync(user, request.Password);
            if (!created.Succeeded) return AccountServiceResult<UserDetailsResponse>.Failure(IdentityError(created));
            IdentityResult assigned = await userManager.AddToRoleAsync(user, request.Role);
            if (!assigned.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AccountServiceResult<UserDetailsResponse>.Failure(IdentityError(assigned));
            }

            AddAudit(actorId.Value, "StaffAccountCreated", user.Id, new { role = request.Role });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Staff account {UserId} created with role {Role} by {ActorId}.", user.Id, request.Role, actorId);
            return await GetUserAsync(user.Id, cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure<UserDetailsResponse>(409, "Dữ liệu đã tồn tại", "Email đã được sử dụng hoặc dữ liệu bị xung đột.");
        }
    }

    public async Task<AccountServiceResult<UserDetailsResponse>> UpdateStaffAsync(
        ClaimsPrincipal actor,
        Guid id,
        UpdateStaffUserRequest request,
        CancellationToken cancellationToken)
    {
        Guid? actorId = GetActorId(actor);
        if (actorId is null) return Failure<UserDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        ApplicationUser? user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Failure<UserDetailsResponse>(404, "Không tìm thấy tài khoản", "Tài khoản không tồn tại.");
        string? role = await GetRoleAsync(user.Id, cancellationToken);
        if (!UserManagementValues.StaffRoles.Contains(role, StringComparer.Ordinal))
            return Failure<UserDetailsResponse>(403, "Không được phép chỉnh sửa", "Module này chỉ chỉnh sửa tài khoản Bác sĩ và Lễ tân.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            string fullName = request.FullName.Trim();
            if (role == SystemRoles.Doctor)
            {
                var doctor = await dbContext.Doctors.SingleOrDefaultAsync(item => item.UserId == id, cancellationToken);
                if (doctor is null) user.StaffFullName = fullName;
                else doctor.UpdateDisplayName(fullName);
            }
            else user.StaffFullName = fullName;
            user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
            IdentityResult updated = await userManager.UpdateAsync(user);
            if (!updated.Succeeded) return AccountServiceResult<UserDetailsResponse>.Failure(IdentityError(updated));
            AddAudit(actorId.Value, "StaffAccountUpdated", id, new { role });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetUserAsync(id, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure<UserDetailsResponse>(409, "Xung đột dữ liệu", "Tài khoản vừa được thay đổi. Vui lòng tải lại trang.");
        }
        catch (DomainRuleException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Validation<UserDetailsResponse>(nameof(request.FullName), exception.Message);
        }
    }

    public async Task<AccountServiceResult> SetActiveAsync(
        ClaimsPrincipal actor,
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        Guid? actorId = GetActorId(actor);
        if (actorId is null) return Failure(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        if (actorId == id) return Failure(409, "Không thể tự thay đổi trạng thái", "Quản trị viên không thể tự vô hiệu hóa tài khoản của mình.");
        ApplicationUser? user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Failure(404, "Không tìm thấy tài khoản", "Tài khoản không tồn tại.");
        string? role = await GetRoleAsync(id, cancellationToken);
        if (role == SystemRoles.Admin) return Failure(403, "Tài khoản được bảo vệ", "Không thể thay đổi trạng thái tài khoản Quản trị viên.");
        if (user.IsActive == isActive) return AccountServiceResult.Success();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            user.IsActive = isActive;
            user.SecurityStamp = Guid.NewGuid().ToString();
            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return AccountServiceResult.Failure(IdentityError(result));
            AddAudit(actorId.Value, isActive ? "StaffAccountActivated" : "StaffAccountDeactivated", id, new { role });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Account {UserId} active state changed to {IsActive} by {ActorId}.", id, isActive, actorId);
            return AccountServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(409, "Xung đột dữ liệu", "Tài khoản vừa được thay đổi. Vui lòng tải lại trang.");
        }
    }

    public async Task<AccountServiceResult> UnlockAsync(
        ClaimsPrincipal actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        Guid? actorId = GetActorId(actor);
        if (actorId is null) return Failure(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        ApplicationUser? user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Failure(404, "Không tìm thấy tài khoản", "Tài khoản không tồn tại.");
        string? role = await GetRoleAsync(id, cancellationToken);
        if (role == SystemRoles.Admin) return Failure(403, "Tài khoản được bảo vệ", "Không thể mở khóa tài khoản Quản trị viên từ module này.");
        if (user.LockoutEnd is null && user.AccessFailedCount == 0) return AccountServiceResult.Success();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.SecurityStamp = Guid.NewGuid().ToString();
            IdentityResult result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return AccountServiceResult.Failure(IdentityError(result));
            AddAudit(actorId.Value, "StaffAccountUnlocked", id, new { role });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AccountServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(409, "Xung đột dữ liệu", "Tài khoản vừa được thay đổi. Vui lòng tải lại trang.");
        }
    }

    private IQueryable<UserRow> BuildRows(DateTimeOffset now) =>
        from user in dbContext.Users.AsNoTracking()
        join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
        join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        let patientName = dbContext.Patients.Where(item => item.UserId == user.Id).Select(item => item.FullName).FirstOrDefault()
        let doctorName = dbContext.Doctors.Where(item => item.UserId == user.Id).Select(item => item.DisplayName).FirstOrDefault()
        let displayName = role.Name == SystemRoles.Patient ? patientName : role.Name == SystemRoles.Doctor ? doctorName ?? user.StaffFullName : user.StaffFullName
        select new UserRow
        {
            Id = user.Id,
            DisplayName = displayName ?? user.Email!,
            Email = user.Email!,
            Role = role.Name!,
            IsActive = user.IsActive,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > now,
            CreatedAtUtc = user.CreatedAtUtc
        };

    private IQueryable<UserDetailsRow> BuildDetailsQuery(DateTimeOffset now) =>
        from user in dbContext.Users.AsNoTracking()
        join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
        join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        let patientName = dbContext.Patients.Where(item => item.UserId == user.Id).Select(item => item.FullName).FirstOrDefault()
        let doctorName = dbContext.Doctors.Where(item => item.UserId == user.Id).Select(item => item.DisplayName).FirstOrDefault()
        let displayName = role.Name == SystemRoles.Patient ? patientName : role.Name == SystemRoles.Doctor ? doctorName ?? user.StaffFullName : user.StaffFullName
        select new UserDetailsRow
        {
            Id = user.Id,
            DisplayName = displayName ?? user.Email!,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            Role = role.Name!,
            IsActive = user.IsActive,
            IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > now,
            LockoutEndUtc = user.LockoutEnd,
            CreatedAtUtc = user.CreatedAtUtc,
            HasDomainProfile = patientName != null || doctorName != null
        };

    private async Task<UserSummaryResponse> BuildSummaryAsync(DateTimeOffset now, CancellationToken token)
    {
        UserSummaryResponse? summary = await BuildRows(now)
            .GroupBy(_ => 1)
            .Select(rows => new UserSummaryResponse(
                rows.Count(),
                rows.Count(x => x.IsActive),
                rows.Count(x => !x.IsActive),
                rows.Count(x => x.IsLockedOut),
                rows.Count(x => x.Role == SystemRoles.Doctor),
                rows.Count(x => x.Role == SystemRoles.Receptionist)))
            .SingleOrDefaultAsync(token);
        return summary ?? new UserSummaryResponse(0, 0, 0, 0, 0, 0);
    }

    private static IQueryable<UserRow> ApplySort(IQueryable<UserRow> rows, string sortBy, bool desc) => (sortBy, desc) switch
    {
        ("name", false) => rows.OrderBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("name", true) => rows.OrderByDescending(x => x.DisplayName).ThenBy(x => x.Id),
        ("email", false) => rows.OrderBy(x => x.Email).ThenBy(x => x.Id),
        ("email", true) => rows.OrderByDescending(x => x.Email).ThenBy(x => x.Id),
        ("role", false) => rows.OrderBy(x => x.Role).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("role", true) => rows.OrderByDescending(x => x.Role).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("status", false) => rows.OrderBy(x => x.IsActive).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("status", true) => rows.OrderByDescending(x => x.IsActive).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("createdAt", false) => rows.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
        _ => rows.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
    };

    private static AccountServiceError? ValidateQuery(UserListQuery query)
    {
        if (!UserManagementValues.PageSizes.Contains(query.PageSize)) return Error(400, "Phân trang không hợp lệ", "Kích thước trang chỉ nhận 10, 20 hoặc 50.");
        if (!UserManagementValues.SortFields.Contains(query.SortBy, StringComparer.Ordinal)) return Error(400, "Sắp xếp không hợp lệ", "Trường sắp xếp không được hỗ trợ.");
        if (query.SortDirection is not ("asc" or "desc")) return Error(400, "Sắp xếp không hợp lệ", "Chiều sắp xếp phải là asc hoặc desc.");
        if (!string.IsNullOrWhiteSpace(query.Role) && !SystemRoles.All.Contains(query.Role, StringComparer.Ordinal)) return Error(400, "Role không hợp lệ", "Role lọc không được hỗ trợ.");
        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status.ToLowerInvariant() is not ("active" or "inactive")) return Error(400, "Trạng thái không hợp lệ", "Trạng thái lọc không được hỗ trợ.");
        if (!string.IsNullOrWhiteSpace(query.Lockout) && query.Lockout.ToLowerInvariant() is not ("locked" or "unlocked")) return Error(400, "Khóa đăng nhập không hợp lệ", "Bộ lọc khóa đăng nhập không được hỗ trợ.");
        return null;
    }

    private async Task<string?> GetRoleAsync(Guid userId, CancellationToken token) => await (
        from userRole in dbContext.UserRoles.AsNoTracking()
        join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        where userRole.UserId == userId
        select role.Name).SingleOrDefaultAsync(token);

    private void AddAudit(Guid actorId, string action, Guid targetId, object metadata) =>
        dbContext.AuditLogs.Add(new AuditLog(Guid.NewGuid(), actorId, action, "ApplicationUser", targetId.ToString(),
            timeProvider.GetUtcNow(), JsonSerializer.Serialize(metadata)));

    private static Guid? GetActorId(ClaimsPrincipal actor) =>
        Guid.TryParse(actor.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : null;
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AccountServiceError Error(int status, string title, string detail) => new(status, title, detail);
    private static AccountServiceResult<T> Failure<T>(int status, string title, string detail) => AccountServiceResult<T>.Failure(Error(status, title, detail));
    private static AccountServiceResult Failure(int status, string title, string detail) => AccountServiceResult.Failure(Error(status, title, detail));
    private static AccountServiceResult<T> Validation<T>(string field, string message) => AccountServiceResult<T>.Failure(new(400, "Dữ liệu không hợp lệ", "Vui lòng kiểm tra dữ liệu.", new Dictionary<string, string[]> { [field] = [message] }));
    private static AccountServiceError IdentityError(IdentityResult result)
    {
        bool duplicate = result.Errors.Any(x => x.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        bool concurrency = result.Errors.Any(x => x.Code.Contains("Concurrency", StringComparison.OrdinalIgnoreCase));
        bool password = result.Errors.Any(x => x.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase));
        if (duplicate) return Error(409, "Email đã tồn tại", "Email đã được sử dụng.");
        if (concurrency) return Error(409, "Xung đột dữ liệu", "Tài khoản vừa được thay đổi. Vui lòng tải lại trang.");
        if (password) return new(400, "Mật khẩu không hợp lệ", "Mật khẩu không đáp ứng chính sách bảo mật.", new Dictionary<string, string[]> { ["Password"] = ["Mật khẩu phải có ít nhất 12 ký tự và 3 ký tự khác nhau."] });
        return Error(400, "Không thể xử lý tài khoản", "Yêu cầu không thể hoàn tất.");
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsLockedOut { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }

    private sealed class UserDetailsRow
    {
        public Guid Id { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public string Role { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsLockedOut { get; init; }
        public DateTimeOffset? LockoutEndUtc { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
        public bool HasDomainProfile { get; init; }
    }
}
