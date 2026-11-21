using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediQueue.Contracts.Profiles;
using MediQueue.Domain.Common;
using MediQueue.Domain.Patients;
using MediQueue.Domain.Time;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Profiles;

public sealed class PatientManagementService(MediQueueDbContext db, TimeProvider clock) : IPatientManagementService
{
    public async Task<AccountServiceResult<PagedPatientsResponse>> GetAsync(PatientListQuery query, CancellationToken token)
    {
        AccountServiceError? validation = Validate(query);
        if (validation is not null) return AccountServiceResult<PagedPatientsResponse>.Failure(validation);
        IQueryable<PatientRow> rows = BuildRows();
        string? search = Text(query.Search);
        if (search is not null) rows = rows.Where(x => x.FullName.Contains(search) || x.Email.Contains(search) || (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));
        rows = query.Status?.ToLowerInvariant() switch
        {
            "active" => rows.Where(x => x.ProfileIsActive && x.AccountIsActive),
            "inactive" => rows.Where(x => !x.ProfileIsActive || !x.AccountIsActive),
            _ => rows
        };
        int totalCount = await rows.CountAsync(token);
        rows = Sort(rows, query.SortBy, query.SortDirection == "desc");
        PatientListItemResponse[] items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new PatientListItemResponse(x.Id, x.UserId, x.FullName, x.Email, x.PhoneNumber,
                x.DateOfBirth, x.ProfileIsActive, x.AccountIsActive, x.AccountCreatedAtUtc)).ToArrayAsync(token);
        var stored = await db.Patients.AsNoTracking().GroupBy(_ => 1)
            .Select(x => new { Total = x.Count(), Active = x.Count(y => y.IsActive) }).SingleOrDefaultAsync(token);
        int total = stored?.Total ?? 0; int active = stored?.Active ?? 0;
        return AccountServiceResult<PagedPatientsResponse>.Success(new(items, query.Page, query.PageSize,
            totalCount, totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize),
            new PatientSummaryResponse(total, active, total - active)));
    }

    public async Task<AccountServiceResult<PatientDetailsResponse>> GetAsync(Guid id, CancellationToken token)
    {
        PatientRow? row = await BuildRows().SingleOrDefaultAsync(x => x.Id == id, token);
        return row is null ? Fail<PatientDetailsResponse>(404, "Không tìm thấy hồ sơ bệnh nhân", "Hồ sơ bệnh nhân không tồn tại.") : AccountServiceResult<PatientDetailsResponse>.Success(ToDetails(row));
    }

    public async Task<AccountServiceResult<PatientDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdatePatientProfileRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<PatientDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        if (!TryRowVersion(request.RowVersion, out byte[] rowVersion)) return Invalid<PatientDetailsResponse>(nameof(request.RowVersion), "Phiên bản dữ liệu không hợp lệ.");
        if (request.DateOfBirth is { } date && date > BusinessTime.GetVietnamServiceDate(clock.GetUtcNow()))
            return Invalid<PatientDetailsResponse>(nameof(request.DateOfBirth), "Ngày sinh không thể nằm trong tương lai.");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            Patient? patient = await db.Patients.SingleOrDefaultAsync(x => x.Id == id, token);
            if (patient is null) return Fail<PatientDetailsResponse>(404, "Không tìm thấy hồ sơ bệnh nhân", "Hồ sơ bệnh nhân không tồn tại.");
            var account = await db.Users.SingleAsync(x => x.Id == patient.UserId, token);
            db.Entry(patient).Property(x => x.RowVersion).OriginalValue = rowVersion;
            patient.UpdateAdministrativeProfile(request.FullName, request.PhoneNumber, request.DateOfBirth);
            account.PhoneNumber = Text(request.PhoneNumber);
            Audit(actorId.Value, "PatientProfileUpdatedByAdmin", id, new { patient.UserId });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return await GetAsync(id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Invalid<PatientDetailsResponse>(nameof(request.FullName), ex.Message); }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(token); return Fail<PatientDetailsResponse>(409, "Xung đột dữ liệu", "Hồ sơ vừa được thay đổi. Vui lòng tải lại trang."); }
    }

    public async Task<AccountServiceResult<PatientSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal actor, CancellationToken token)
    {
        Guid? userId = ActorId(actor);
        if (userId is null) return Fail<PatientSelfProfileResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        PatientRow? row = await BuildRows().SingleOrDefaultAsync(x => x.UserId == userId.Value, token);
        PatientDetailsResponse? profile = row is null ? null : ToDetails(row);
        return AccountServiceResult<PatientSelfProfileResponse>.Success(new(profile is not null, profile));
    }

    private IQueryable<PatientRow> BuildRows() =>
        from patient in db.Patients.AsNoTracking()
        join user in db.Users.AsNoTracking() on patient.UserId equals user.Id
        select new PatientRow
        {
            Id = patient.Id,
            UserId = patient.UserId,
            FullName = patient.FullName,
            Email = user.Email!,
            PhoneNumber = patient.PhoneNumber ?? user.PhoneNumber,
            DateOfBirth = patient.DateOfBirth,
            ProfileIsActive = patient.IsActive,
            AccountIsActive = user.IsActive,
            AccountCreatedAtUtc = user.CreatedAtUtc,
            RowVersion = patient.RowVersion
        };

    private static PatientDetailsResponse ToDetails(PatientRow x) => new(
        x.Id, x.UserId, x.FullName, x.Email, x.PhoneNumber, x.DateOfBirth, x.ProfileIsActive,
        x.AccountIsActive, x.AccountCreatedAtUtc, Convert.ToBase64String(x.RowVersion));

    private static IQueryable<PatientRow> Sort(IQueryable<PatientRow> rows, string field, bool desc) => (field, desc) switch
    {
        ("email", false) => rows.OrderBy(x => x.Email).ThenBy(x => x.Id),
        ("email", true) => rows.OrderByDescending(x => x.Email).ThenBy(x => x.Id),
        ("status", false) => rows.OrderBy(x => x.ProfileIsActive && x.AccountIsActive).ThenBy(x => x.FullName).ThenBy(x => x.Id),
        ("status", true) => rows.OrderByDescending(x => x.ProfileIsActive && x.AccountIsActive).ThenBy(x => x.FullName).ThenBy(x => x.Id),
        ("createdAt", false) => rows.OrderBy(x => x.AccountCreatedAtUtc).ThenBy(x => x.Id),
        ("createdAt", true) => rows.OrderByDescending(x => x.AccountCreatedAtUtc).ThenBy(x => x.Id),
        ("name", true) => rows.OrderByDescending(x => x.FullName).ThenBy(x => x.Id),
        _ => rows.OrderBy(x => x.FullName).ThenBy(x => x.Id)
    };

    private static AccountServiceError? Validate(PatientListQuery q)
    {
        if (q.Page < 1 || !PatientManagementValues.PageSizes.Contains(q.PageSize)) return Error(400, "Phân trang không hợp lệ", "Trang phải từ 1 và kích thước trang chỉ nhận 10, 20 hoặc 50.");
        if (!PatientManagementValues.SortFields.Contains(q.SortBy, StringComparer.Ordinal)) return Error(400, "Sắp xếp không hợp lệ", "Trường sắp xếp không được hỗ trợ.");
        if (q.SortDirection is not ("asc" or "desc")) return Error(400, "Sắp xếp không hợp lệ", "Chiều sắp xếp phải là asc hoặc desc.");
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status.ToLowerInvariant() is not ("active" or "inactive")) return Error(400, "Trạng thái không hợp lệ", "Bộ lọc trạng thái không được hỗ trợ.");
        return null;
    }

    private void Audit(Guid actorId, string action, Guid id, object metadata) => db.AuditLogs.Add(new AuditLog(Guid.NewGuid(), actorId, action, "Patient", id.ToString(), clock.GetUtcNow(), JsonSerializer.Serialize(metadata)));
    private static Guid? ActorId(ClaimsPrincipal actor) => Guid.TryParse(actor.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : null;
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool TryRowVersion(string value, out byte[] bytes) { try { bytes = Convert.FromBase64String(value); return bytes.Length > 0; } catch (FormatException) { bytes = []; return false; } }
    private static AccountServiceError Error(int status, string title, string detail) => new(status, title, detail);
    private static AccountServiceResult<T> Fail<T>(int status, string title, string detail) => AccountServiceResult<T>.Failure(Error(status, title, detail));
    private static AccountServiceResult<T> Invalid<T>(string field, string message) => AccountServiceResult<T>.Failure(new(400, "Dữ liệu không hợp lệ", "Vui lòng kiểm tra dữ liệu.", new Dictionary<string, string[]> { [field] = [message] }));

    private sealed class PatientRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty; public string? PhoneNumber { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public bool ProfileIsActive { get; init; }
        public bool AccountIsActive { get; init; }
        public DateTimeOffset AccountCreatedAtUtc { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }
}
