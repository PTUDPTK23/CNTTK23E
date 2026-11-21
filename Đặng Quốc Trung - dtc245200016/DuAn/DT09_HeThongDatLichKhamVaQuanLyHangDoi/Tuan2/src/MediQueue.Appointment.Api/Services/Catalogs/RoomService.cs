using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MediQueue.Contracts.Catalogs;
using MediQueue.Domain.Common;
using MediQueue.Domain.Rooms;
using MediQueue.Infrastructure.Persistence;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediQueue.Appointment.Api.Services.Catalogs;

public sealed class RoomService(MediQueueDbContext db, TimeProvider clock) : IRoomService
{
    public async Task<AccountServiceResult<PagedRoomsResponse>> GetAsync(RoomListQuery query, CancellationToken token)
    {
        AccountServiceError? error = Validate(query);
        if (error is not null) return AccountServiceResult<PagedRoomsResponse>.Failure(error);
        IQueryable<Room> rows = db.Rooms.AsNoTracking();
        string? search = Text(query.Search);
        if (search is not null) rows = rows.Where(x => x.Code.Contains(search) || x.Name.Contains(search) || (x.Location != null && x.Location.Contains(search)));
        rows = query.Status?.ToLowerInvariant() switch { "active" => rows.Where(x => x.IsActive), "inactive" => rows.Where(x => !x.IsActive), _ => rows };
        int count = await rows.CountAsync(token);
        rows = Sort(rows, query.SortBy, query.SortDirection == "desc");
        RoomListItemResponse[] items = await rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new RoomListItemResponse(x.Id, x.Code, x.Name, x.Location, x.IsActive, x.DoctorSchedules.Count, x.TimeSlots.Count)).ToArrayAsync(token);
        RoomSummaryResponse summary = await db.Rooms.AsNoTracking().GroupBy(_ => 1)
            .Select(x => new RoomSummaryResponse(x.Count(), x.Count(y => y.IsActive), x.Count(y => !y.IsActive)))
            .SingleOrDefaultAsync(token) ?? new(0, 0, 0);
        return AccountServiceResult<PagedRoomsResponse>.Success(new(items, query.Page, query.PageSize, count,
            count == 0 ? 0 : (int)Math.Ceiling(count / (double)query.PageSize), summary));
    }

    public async Task<AccountServiceResult<RoomDetailsResponse>> GetAsync(Guid id, CancellationToken token)
    {
        RoomDetailsResponse? item = await db.Rooms.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new RoomDetailsResponse(x.Id, x.Code, x.Name, x.Location, x.IsActive,
                x.DoctorSchedules.Count, x.DoctorSchedules.Count(y => y.IsActive), x.TimeSlots.Count, x.TimeSlots.Count(y => y.IsActive)))
            .SingleOrDefaultAsync(token);
        return item is null ? Fail<RoomDetailsResponse>(404, "Không tìm thấy phòng khám", "Phòng khám không tồn tại.") : AccountServiceResult<RoomDetailsResponse>.Success(item);
    }

    public async Task<AccountServiceResult<RoomDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateRoomRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<RoomDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            string code = request.Code.Trim().ToUpperInvariant(); string name = request.Name.Trim();
            if (await db.Rooms.AnyAsync(x => x.Code == code || x.Name == name, token)) return Duplicate<RoomDetailsResponse>();
            var item = new Room(Guid.NewGuid(), code, name, request.Location);
            db.Rooms.Add(item); Audit(actorId.Value, "RoomCreated", item.Id, new { item.Code, item.Name, item.Location });
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return await GetAsync(item.Id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Fail<RoomDetailsResponse>(400, "Dữ liệu không hợp lệ", ex.Message); }
        catch (DbUpdateException) { await transaction.RollbackAsync(token); return Duplicate<RoomDetailsResponse>(); }
    }

    public async Task<AccountServiceResult<RoomDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateRoomRequest request, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail<RoomDetailsResponse>(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            Room? item = await db.Rooms.SingleOrDefaultAsync(x => x.Id == id, token);
            if (item is null) return Fail<RoomDetailsResponse>(404, "Không tìm thấy phòng khám", "Phòng khám không tồn tại.");
            string code = request.Code.Trim().ToUpperInvariant(); string name = request.Name.Trim();
            if (await db.Rooms.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name), token)) return Duplicate<RoomDetailsResponse>();
            item.Update(code, name, request.Location); Audit(actorId.Value, "RoomUpdated", id, new { item.Code, item.Name, item.Location });
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return await GetAsync(id, token);
        }
        catch (DomainRuleException ex) { await transaction.RollbackAsync(token); return Fail<RoomDetailsResponse>(400, "Dữ liệu không hợp lệ", ex.Message); }
        catch (DbUpdateException) { await transaction.RollbackAsync(token); return Duplicate<RoomDetailsResponse>(); }
    }

    public async Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal actor, Guid id, bool active, CancellationToken token)
    {
        Guid? actorId = ActorId(actor);
        if (actorId is null) return Fail(401, "Chưa xác thực", "Phiên đăng nhập không hợp lệ.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            Room? item = await db.Rooms.SingleOrDefaultAsync(x => x.Id == id, token);
            if (item is null) return Fail(404, "Không tìm thấy phòng khám", "Phòng khám không tồn tại.");
            if (!active && (await db.DoctorSchedules.AnyAsync(x => x.RoomId == id && x.IsActive, token) || await db.TimeSlots.AnyAsync(x => x.RoomId == id && x.IsActive, token)))
                return Fail(409, "Phòng khám đang được sử dụng", "Hãy xử lý lịch bác sĩ và khung giờ đang hoạt động trước khi vô hiệu hóa phòng.");
            if (item.IsActive == active) return AccountServiceResult.Success();
            if (active) item.Activate(); else item.Deactivate();
            Audit(actorId.Value, active ? "RoomActivated" : "RoomDeactivated", id, new { item.Code });
            await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return AccountServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(token); return Fail(409, "Xung đột dữ liệu", "Dữ liệu vừa được thay đổi. Vui lòng tải lại trang."); }
    }

    private static IQueryable<Room> Sort(IQueryable<Room> rows, string field, bool desc) => (field, desc) switch
    {
        ("name", false) => rows.OrderBy(x => x.Name).ThenBy(x => x.Id),
        ("name", true) => rows.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
        ("location", false) => rows.OrderBy(x => x.Location).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("location", true) => rows.OrderByDescending(x => x.Location).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("status", false) => rows.OrderBy(x => x.IsActive).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("status", true) => rows.OrderByDescending(x => x.IsActive).ThenBy(x => x.Code).ThenBy(x => x.Id),
        ("code", true) => rows.OrderByDescending(x => x.Code).ThenBy(x => x.Id),
        _ => rows.OrderBy(x => x.Code).ThenBy(x => x.Id)
    };
    private static AccountServiceError? Validate(RoomListQuery q)
    {
        if (q.Page < 1) return Error(400, "Phân trang không hợp lệ", "Số trang phải từ 1.");
        if (!RoomManagementValues.PageSizes.Contains(q.PageSize)) return Error(400, "Phân trang không hợp lệ", "Kích thước trang chỉ nhận 10, 20 hoặc 50.");
        if (!RoomManagementValues.SortFields.Contains(q.SortBy, StringComparer.Ordinal)) return Error(400, "Sắp xếp không hợp lệ", "Trường sắp xếp không được hỗ trợ.");
        if (q.SortDirection is not ("asc" or "desc")) return Error(400, "Sắp xếp không hợp lệ", "Chiều sắp xếp phải là asc hoặc desc.");
        if (!string.IsNullOrWhiteSpace(q.Status) && q.Status.ToLowerInvariant() is not ("active" or "inactive")) return Error(400, "Trạng thái không hợp lệ", "Bộ lọc trạng thái không được hỗ trợ.");
        return null;
    }
    private void Audit(Guid actorId, string action, Guid id, object metadata) => db.AuditLogs.Add(new AuditLog(Guid.NewGuid(), actorId, action, "Room", id.ToString(), clock.GetUtcNow(), JsonSerializer.Serialize(metadata)));
    private static Guid? ActorId(ClaimsPrincipal actor) => Guid.TryParse(actor.FindFirstValue(JwtRegisteredClaimNames.Sub), out Guid id) ? id : null;
    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AccountServiceError Error(int status, string title, string detail) => new(status, title, detail);
    private static AccountServiceResult<T> Fail<T>(int status, string title, string detail) => AccountServiceResult<T>.Failure(Error(status, title, detail));
    private static AccountServiceResult Fail(int status, string title, string detail) => AccountServiceResult.Failure(Error(status, title, detail));
    private static AccountServiceResult<T> Duplicate<T>() => Fail<T>(409, "Phòng khám đã tồn tại", "Mã hoặc tên phòng khám đã được sử dụng.");
}
