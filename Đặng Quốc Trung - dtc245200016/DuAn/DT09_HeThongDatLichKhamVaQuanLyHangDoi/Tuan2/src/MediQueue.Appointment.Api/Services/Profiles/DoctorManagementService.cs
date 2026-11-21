using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediQueue.Contracts.Profiles;
using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;
using MediQueue.Domain.Security;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Profiles;

public sealed class DoctorManagementService(MediQueueDbContext db, TimeProvider clock) : IDoctorManagementService
{
    public async Task<AccountServiceResult<PagedDoctorsResponse>> GetAsync(DoctorListQuery query, CancellationToken token)
    {
        AccountServiceError? validation = Validate(query);
        if (validation is not null) return AccountServiceResult<PagedDoctorsResponse>.Failure(validation);

        IQueryable<DoctorRow> rows = BuildRows();
        string? search = Text(query.Search);
        if (search is not null)
            rows = rows.Where(x => x.DisplayName.Contains(search) || x.Email.Contains(search) || x.DepartmentName.Contains(search));
        if (query.DepartmentId.HasValue) rows = rows.Where(x => x.DepartmentId == query.DepartmentId.Value);
        rows = query.Status?.ToLowerInvariant() switch
        {
            "active" => rows.Where(x => x.ProfileIsActive && x.AccountIsActive),
            "inactive" => rows.Where(x => !x.ProfileIsActive || !x.AccountIsActive),
            _ => rows
        };

        int totalCount = await rows.CountAsync(token);
        rows = Sort(rows, query.SortBy, query.SortDirection == "desc");
        DoctorListItemResponse[] items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new DoctorListItemResponse(x.Id, x.UserId, x.DisplayName, x.DepartmentId,
                x.DepartmentCode, x.DepartmentName, x.Email, x.ProfileIsActive, x.AccountIsActive, x.AccountCreatedAtUtc))
            .ToArrayAsync(token);

        DoctorSummaryResponse summary = await BuildSummaryAsync(token);
        DoctorDepartmentOptionResponse[] departments = await ActiveDepartments().ToArrayAsync(token);
        return AccountServiceResult<PagedDoctorsResponse>.Success(new(items, query.Page, query.PageSize,
            totalCount, totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize), summary, departments));
    }

    public async Task<AccountServiceResult<DoctorDetailsResponse>> GetAsync(Guid id, CancellationToken token)
    {
        DoctorRow? row = await BuildRows().SingleOrDefaultAsync(x => x.Id == id, token);
        return row is null ? Fail<DoctorDetailsResponse>(404, "Không tìm thấy hồ sơ bác sĩ", "Hồ sơ bác sĩ không tồn tại.") : AccountServiceResult<DoctorDetailsResponse>.Success(ToDetails(row));
    }

    public async Task<AccountServiceResult<DoctorCreateOptionsResponse>> GetCreateOptionsAsync(CancellationToken token)
    {
        AvailableDoctorAccountResponse[] accounts = await AvailableAccounts().ToArrayAsync(token);
        DoctorDepartmentOptionResponse[] departments = await ActiveDepartments().ToArrayAsync(token);
        return AccountServiceResult<DoctorCreateOptionsResponse>.Success(new(accounts, departments));
    }

    public async Task<AccountServiceResult<DoctorDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateDoctorProfileRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<DoctorDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            var account = await db.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, token);
            if (account is null) return Invalid<DoctorDetailsResponse>(nameof(request.UserId), "Tài khoản không tồn tại.");
            if (!account.IsActive) return Fail<DoctorDetailsResponse>(409, "Tài khoản chưa hoạt động", "Không thể liên kết hồ sơ với tài khoản đã bị vô hiệu hóa.");
            string? role = await RoleOfAsync(request.UserId, token);
            if (role != SystemRoles.Doctor) return Invalid<DoctorDetailsResponse>(nameof(request.UserId), "Chỉ tài khoản có vai trò Bác sĩ mới được liên kết.");
            if (await db.Doctors.AnyAsync(x => x.UserId == request.UserId, token))
                return Fail<DoctorDetailsResponse>(409, "Hồ sơ đã được liên kết", "Tài khoản Bác sĩ đã có hồ sơ nghiệp vụ.");
            if (!await db.Departments.AnyAsync(x => x.Id == request.DepartmentId && x.IsActive, token))
                return Invalid<DoctorDetailsResponse>(nameof(request.DepartmentId), "Chuyên khoa không tồn tại hoặc đã bị vô hiệu hóa.");

            var doctor = new Doctor(Guid.NewGuid(), request.UserId, request.DepartmentId, request.DisplayName);
            db.Doctors.Add(doctor);
            Audit(actorId.Value, "DoctorProfileCreated", doctor.Id, new { doctor.UserId, doctor.DepartmentId });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return await GetAsync(doctor.Id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Invalid<DoctorDetailsResponse>(nameof(request.DisplayName), ex.Message); }
        catch (DbUpdateException) { await transaction.RollbackAsync(token); return Fail<DoctorDetailsResponse>(409, "Xung đột liên kết", "Tài khoản đã được liên kết hoặc dữ liệu vừa thay đổi."); }
    }

    public async Task<AccountServiceResult<DoctorDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateDoctorProfileRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<DoctorDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        if (!TryRowVersion(request.RowVersion, out byte[] rowVersion)) return Invalid<DoctorDetailsResponse>(nameof(request.RowVersion), "Phiên bản dữ liệu không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            Doctor? doctor = await db.Doctors.SingleOrDefaultAsync(x => x.Id == id, token);
            if (doctor is null) return Fail<DoctorDetailsResponse>(404, "Không tìm thấy hồ sơ bác sĩ", "Hồ sơ bác sĩ không tồn tại.");
            if (!await db.Departments.AnyAsync(x => x.Id == request.DepartmentId && x.IsActive, token))
                return Invalid<DoctorDetailsResponse>(nameof(request.DepartmentId), "Chuyên khoa không tồn tại hoặc đã bị vô hiệu hóa.");
            Guid oldDepartmentId = doctor.DepartmentId;
            db.Entry(doctor).Property(x => x.RowVersion).OriginalValue = rowVersion;
            doctor.UpdateProfile(request.DisplayName, request.DepartmentId);
            Audit(actorId.Value, "DoctorProfileUpdated", id, new { oldDepartmentId, doctor.DepartmentId });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return await GetAsync(id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Invalid<DoctorDetailsResponse>(nameof(request.DisplayName), ex.Message); }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(token); return Fail<DoctorDetailsResponse>(409, "Xung đột dữ liệu", "Hồ sơ vừa được thay đổi. Vui lòng tải lại trang."); }
    }

    public async Task<AccountServiceResult<DoctorSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal actor, CancellationToken token)
    {
        Guid? userId = ActorId(actor);
        if (userId is null) return Fail<DoctorSelfProfileResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        DoctorRow? row = await BuildRows().SingleOrDefaultAsync(x => x.UserId == userId.Value, token);
        DoctorDetailsResponse? profile = row is null ? null : ToDetails(row);
        return AccountServiceResult<DoctorSelfProfileResponse>.Success(new(profile is not null, profile));
    }

    private IQueryable<DoctorRow> BuildRows() =>
        from doctor in db.Doctors.AsNoTracking()
        join user in db.Users.AsNoTracking() on doctor.UserId equals user.Id
        join department in db.Departments.AsNoTracking() on doctor.DepartmentId equals department.Id
        select new DoctorRow
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            DisplayName = doctor.DisplayName,
            DepartmentId = department.Id,
            DepartmentCode = department.Code,
            DepartmentName = department.Name,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            ProfileIsActive = doctor.IsActive,
            AccountIsActive = user.IsActive,
            AccountCreatedAtUtc = user.CreatedAtUtc,
            RowVersion = doctor.RowVersion
        };

    private static DoctorDetailsResponse ToDetails(DoctorRow x) => new(
        x.Id, x.UserId, x.DisplayName, x.DepartmentId, x.DepartmentCode, x.DepartmentName,
        x.Email, x.PhoneNumber, x.ProfileIsActive, x.AccountIsActive, x.AccountCreatedAtUtc,
        Convert.ToBase64String(x.RowVersion));

    private IQueryable<AvailableDoctorAccountResponse> AvailableAccounts() =>
        from user in db.Users.AsNoTracking()
        join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
        join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        where role.Name == SystemRoles.Doctor && user.IsActive && !db.Doctors.Any(x => x.UserId == user.Id)
        orderby user.StaffFullName ?? user.Email, user.Id
        select new AvailableDoctorAccountResponse(user.Id, user.StaffFullName ?? user.Email!, user.Email!, user.PhoneNumber, user.CreatedAtUtc);

    private IQueryable<DoctorDepartmentOptionResponse> ActiveDepartments() => db.Departments.AsNoTracking()
        .Where(x => x.IsActive).OrderBy(x => x.Name).ThenBy(x => x.Id)
        .Select(x => new DoctorDepartmentOptionResponse(x.Id, x.Code, x.Name));

    private async Task<DoctorSummaryResponse> BuildSummaryAsync(CancellationToken token)
    {
        var summary = await db.Doctors.AsNoTracking().GroupBy(_ => 1)
            .Select(x => new { Total = x.Count(), Active = x.Count(y => y.IsActive) }).SingleOrDefaultAsync(token);
        int unlinked = await AvailableAccounts().CountAsync(token);
        int total = summary?.Total ?? 0; int active = summary?.Active ?? 0;
        return new(total, active, total - active, unlinked);
    }

    private static IQueryable<DoctorRow> Sort(IQueryable<DoctorRow> rows, string field, bool desc) => (field, desc) switch
    {
        ("department", false) => rows.OrderBy(x => x.DepartmentName).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("department", true) => rows.OrderByDescending(x => x.DepartmentName).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("email", false) => rows.OrderBy(x => x.Email).ThenBy(x => x.Id),
        ("email", true) => rows.OrderByDescending(x => x.Email).ThenBy(x => x.Id),
        ("status", false) => rows.OrderBy(x => x.ProfileIsActive && x.AccountIsActive).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("status", true) => rows.OrderByDescending(x => x.ProfileIsActive && x.AccountIsActive).ThenBy(x => x.DisplayName).ThenBy(x => x.Id),
        ("createdAt", false) => rows.OrderBy(x => x.AccountCreatedAtUtc).ThenBy(x => x.Id),
        ("createdAt", true) => rows.OrderByDescending(x => x.AccountCreatedAtUtc).ThenBy(x => x.Id),
        ("name", true) => rows.OrderByDescending(x => x.DisplayName).ThenBy(x => x.Id),
        _ => rows.OrderBy(x => x.DisplayName).ThenBy(x => x.Id)
    };

    private static AccountServiceError? Validate(DoctorListQuery q)
    {
        if (q.Page < 1 || !DoctorManagementValues.PageSizes.Contains(q.PageSize)) return Error(400, "Phân trang không hợp lệ", "Trang phải từ 1 và kích thước trang chỉ nhận 10, 20 hoặc 50.");
        if (!DoctorManagementValues.SortFields.Contains(q.SortBy, StringComparer.Ordinal)) return Error(400, "Sắp xếp không hợp lệ", "Trường sắp xếp không được hỗ trợ.");
        if (q.SortDirection is not ("asc" or "desc")) return Error(400, "Sắp xếp không hợp lệ", "Chiều sắp xếp phải là asc hoặc desc.");
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status.ToLowerInvariant() is not ("active" or "inactive")) return Error(400, "Trạng thái không hợp lệ", "Bộ lọc trạng thái không được hỗ trợ.");
        return null;
    }

    private async Task<string?> RoleOfAsync(Guid userId, CancellationToken token) => await (
        from userRole in db.UserRoles.AsNoTracking()
        join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
        where userRole.UserId == userId
        select role.Name).SingleOrDefaultAsync(token);
    private void Audit(Guid actorId, string action, Guid id, object metadata) => db.AuditLogs.Add(new AuditLog(Guid.NewGuid(), actorId, action, "Doctor", id.ToString(), clock.GetUtcNow(), JsonSerializer.Serialize(metadata)));
    private static Guid? ActorId(ClaimsPrincipal actor) => Guid.TryParse(actor.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : null;
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool TryRowVersion(string value, out byte[] bytes) { try { bytes = Convert.FromBase64String(value); return bytes.Length > 0; } catch (FormatException) { bytes = []; return false; } }
    private static AccountServiceError Error(int status, string title, string detail) => new(status, title, detail);
    private static AccountServiceResult<T> Fail<T>(int status, string title, string detail) => AccountServiceResult<T>.Failure(Error(status, title, detail));
    private static AccountServiceResult<T> Invalid<T>(string field, string message) => AccountServiceResult<T>.Failure(new(400, "Dữ liệu không hợp lệ", "Vui lòng kiểm tra dữ liệu.", new Dictionary<string, string[]> { [field] = [message] }));

    private sealed class DoctorRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public Guid DepartmentId { get; init; }
        public string DepartmentCode { get; init; } = string.Empty; public string DepartmentName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty; public string? PhoneNumber { get; init; }
        public bool ProfileIsActive { get; init; }
        public bool AccountIsActive { get; init; }
        public DateTimeOffset AccountCreatedAtUtc { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }
}
