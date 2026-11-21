using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediQueue.Contracts.Catalogs;
using MediQueue.Domain.Common;
using MediQueue.Domain.Departments;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Catalogs;

public sealed class DepartmentService(MediQueueDbContext db, TimeProvider clock) : IDepartmentService
{
    public async Task<AccountServiceResult<PagedDepartmentsResponse>> GetAsync(DepartmentListQuery query, CancellationToken token)
    {
        AccountServiceError? error = Validate(query);
        if (error is not null) return AccountServiceResult<PagedDepartmentsResponse>.Failure(error);
        IQueryable<Department> rows = db.Departments.AsNoTracking();
        string? search = Text(query.Search);
        if (search is not null) rows = rows.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        rows = query.Status?.ToLowerInvariant() switch
        {
            "active" => rows.Where(x => x.IsActive),
            "inactive" => rows.Where(x => !x.IsActive),
            _ => rows
        };
        int count = await rows.CountAsync(token);
        rows = Sort(rows, query.SortBy, query.SortDirection == "desc");
        DepartmentListItemResponse[] items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new DepartmentListItemResponse(x.Id, x.Code, x.Name, x.IsActive, x.Doctors.Count))
            .ToArrayAsync(token);
        DepartmentSummaryResponse summary = await db.Departments.AsNoTracking().GroupBy(_ => 1)
            .Select(x => new DepartmentSummaryResponse(x.Count(), x.Count(y => y.IsActive), x.Count(y => !y.IsActive)))
            .SingleOrDefaultAsync(token) ?? new(0, 0, 0);
        return AccountServiceResult<PagedDepartmentsResponse>.Success(new(
            items, query.Page, query.PageSize, count, count == 0 ? 0 : (int)Math.Ceiling(count / (double)query.PageSize), summary));
    }

    public async Task<AccountServiceResult<DepartmentDetailsResponse>> GetAsync(Guid id, CancellationToken token)
    {
        DepartmentDetailsResponse? item = await db.Departments.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new DepartmentDetailsResponse(x.Id, x.Code, x.Name, x.IsActive, x.Doctors.Count, x.Doctors.Count(y => y.IsActive)))
            .SingleOrDefaultAsync(token);
        return item is null ? Fail<DepartmentDetailsResponse>(404, "Không tìm thấy chuyên khoa", "Chuyên khoa không tồn tại.") : AccountServiceResult<DepartmentDetailsResponse>.Success(item);
    }

    public async Task<AccountServiceResult<DepartmentDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateDepartmentRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<DepartmentDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            string code = request.Code.Trim().ToUpperInvariant();
            string name = request.Name.Trim();
            if (await db.Departments.AnyAsync(x => x.Code == code || x.Name == name, token))
                return Fail<DepartmentDetailsResponse>(409, "Chuyên khoa đã tồn tại", "Mã hoặc tên chuyên khoa đã được sử dụng.");
            var item = new Department(Guid.NewGuid(), code, name);
            db.Departments.Add(item);
            Audit(actorId.Value, "DepartmentCreated", item.Id, new { item.Code, item.Name });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return await GetAsync(item.Id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Invalid<DepartmentDetailsResponse>(ex.Message); }
        catch (DbUpdateException) { await transaction.RollbackAsync(token); return Conflict<DepartmentDetailsResponse>(); }
    }

    public async Task<AccountServiceResult<DepartmentDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateDepartmentRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<DepartmentDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            Department? item = await db.Departments.SingleOrDefaultAsync(x => x.Id == id, token);
            if (item is null) return Fail<DepartmentDetailsResponse>(404, "Không tìm thấy chuyên khoa", "Chuyên khoa không tồn tại.");
            string code = request.Code.Trim().ToUpperInvariant();
            string name = request.Name.Trim();
            if (await db.Departments.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name), token))
                return Fail<DepartmentDetailsResponse>(409, "Chuyên khoa đã tồn tại", "Mã hoặc tên chuyên khoa đã được sử dụng.");
            item.Update(code, name);
            Audit(actorId.Value, "DepartmentUpdated", id, new { item.Code, item.Name });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return await GetAsync(id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Invalid<DepartmentDetailsResponse>(ex.Message); }
        catch (DbUpdateException) { await transaction.RollbackAsync(token); return Conflict<DepartmentDetailsResponse>(); }
    }

    public async Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal actor, Guid id, bool active, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            Department? item = await db.Departments.SingleOrDefaultAsync(x => x.Id == id, token);
            if (item is null) return Fail(404, "Không tìm thấy chuyên khoa", "Chuyên khoa không tồn tại.");
            if (!active && await db.Doctors.AnyAsync(x => x.DepartmentId == id && x.IsActive, token))
                return Fail(409, "Chuyên khoa đang được sử dụng", "Hãy xử lý các bác sĩ đang hoạt động trước khi vô hiệu hóa chuyên khoa.");
            if (item.IsActive == active) return AccountServiceResult.Success();
            if (active) item.Activate(); else item.Deactivate();
            Audit(actorId.Value, active ? "DepartmentActivated" : "DepartmentDeactivated", id, new { item.Code });
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return AccountServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(token); return Fail(409, "Xung đột dữ liệu", "Dữ liệu vừa được thay đổi. Vui lòng tải lại trang."); }
    }

    private static IQueryable<Department> Sort(IQueryable<Department> rows, string field, bool desc) => (field, desc) switch
    {
        ("name", false) => rows.OrderBy(x => x.Name).ThenBy(x => x.Id),
        ("name", true) => rows.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
        ("status", false) => rows.OrderBy(x => x.IsActive).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("status", true) => rows.OrderByDescending(x => x.IsActive).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("code", true) => rows.OrderByDescending(x => x.Code).ThenBy(x => x.Id),
        _ => rows.OrderBy(x => x.Code).ThenBy(x => x.Id)
    };

    private static AccountServiceError? Validate(DepartmentListQuery q)
    {
        if (q.Page < 1) return Error(400, "Phân trang không hợp lệ", "Số trang phải từ 1.");
        if (!DepartmentManagementValues.PageSizes.Contains(q.PageSize)) return Error(400, "Phân trang không hợp lệ", "Kích thước trang chỉ nhận 10, 20 hoặc 50.");
        if (!DepartmentManagementValues.SortFields.Contains(q.SortBy, StringComparer.Ordinal)) return Error(400, "Sắp xếp không hợp lệ", "Trường sắp xếp không được hỗ trợ.");
        if (q.SortDirection is not ("asc" or "desc")) return Error(400, "Sắp xếp không hợp lệ", "Chiều sắp xếp phải là asc hoặc desc.");
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status.ToLowerInvariant() is not ("active" or "inactive")) return Error(400, "Trạng thái không hợp lệ", "Bộ lọc trạng thái không được hỗ trợ.");
        return null;
    }

    private void Audit(Guid actorId, string action, Guid id, object metadata) => db.AuditLogs.Add(new AuditLog(
        Guid.NewGuid(), actorId, action, "Department", id.ToString(), clock.GetUtcNow(), JsonSerializer.Serialize(metadata)));
    private static Guid? ActorId(ClaimsPrincipal actor) => Guid.TryParse(actor.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : null;
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AccountServiceError Error(int status, string title, string detail) => new(status, title, detail);
    private static AccountServiceResult<T> Fail<T>(int status, string title, string detail) => AccountServiceResult<T>.Failure(Error(status, title, detail));
    private static AccountServiceResult Fail(int status, string title, string detail) => AccountServiceResult.Failure(Error(status, title, detail));
    private static AccountServiceResult<T> Invalid<T>(string detail) => Fail<T>(400, "Dữ liệu không hợp lệ", detail);
    private static AccountServiceResult<T> Conflict<T>() => Fail<T>(409, "Xung đột dữ liệu", "Mã hoặc tên chuyên khoa đã được sử dụng.");
}
