using Library.Api;
using Library.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LibraryDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Library")));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<ReservationPublisher>();

var app = builder.Build();
await InitDatabaseAsync(app);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ======================= BOOKS =======================
app.MapGet("/books", async (string? keyword, LibraryDbContext db) =>
{
    var q = db.Books.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(keyword))
        q = q.Where(b => EF.Functions.ILike(b.Title, $"%{keyword}%") ||
                         EF.Functions.ILike(b.Author, $"%{keyword}%"));
    var books = await q.OrderBy(b => b.Id).ToListAsync();
    return Results.Ok(books.Select(BookDto.From));                        // 200
});

app.MapGet("/books/{id:int:min(1)}", async (int id, LibraryDbContext db) =>
{
    var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    return book is null
        ? Results.NotFound(new { error = $"Không tìm thấy sách {id}" })    // 404
        : Results.Ok(BookDto.From(book));                                  // 200 (có AvailableCopies)
});

// ===================== RESERVATIONS =====================
app.MapPost("/reservations", async (CreateReservationRequest req, LibraryDbContext db,
    ReservationPublisher publisher, ILogger<Program> log, CancellationToken ct) =>
{
    // 1. Validation -> 400
    var errors = new Dictionary<string, string[]>();
    if (req.UserId <= 0) errors["userId"] = ["userId phải lớn hơn 0"];
    if (req.BookId <= 0) errors["bookId"] = ["bookId phải lớn hơn 0"];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    // 2. Sách / người dùng phải tồn tại -> 404
    var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == req.BookId, ct);
    if (book is null) return Results.NotFound(new { error = $"Không tìm thấy sách {req.BookId}" });
    if (!await db.Users.AnyAsync(u => u.Id == req.UserId, ct))
        return Results.NotFound(new { error = $"Không tìm thấy người dùng {req.UserId}" });

    // 3. Lưu trạng thái Pending TRƯỚC khi publish (để worker chắc chắn thấy bản ghi)
    var now = DateTime.UtcNow;
    var reservation = new Reservation
    {
        Id = Guid.NewGuid(), UserId = req.UserId, BookId = req.BookId,
        Status = ReservationStatus.Pending, CreatedAt = now, UpdatedAt = now
    };
    db.Reservations.Add(reservation);
    await db.SaveChangesAsync(ct);

    // 4. Đưa yêu cầu vào RabbitMQ
    try
    {
        await publisher.PublishAsync(
            new ReservationRequested(reservation.Id, reservation.UserId, reservation.BookId, now), ct);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Không publish được reservation {ReservationId}", reservation.Id);
        reservation.Status = ReservationStatus.Rejected;
        reservation.Reason = "Hàng đợi không khả dụng";
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(CancellationToken.None);
        return Results.Problem("Hàng đợi tạm thời không khả dụng, vui lòng thử lại", statusCode: 503);
    }

    log.LogInformation("Accepted reservation {ReservationId} (user {UserId}, book {BookId})",
        reservation.Id, reservation.UserId, reservation.BookId);
    return Results.Created($"/reservations/{reservation.Id}",             // 201 + Location
        ReservationDto.From(reservation, book.Title));
});

app.MapGet("/reservations/{id:guid}", async (Guid id, LibraryDbContext db) =>
{
    var r = await db.Reservations.AsNoTracking().Include(x => x.Book)
        .FirstOrDefaultAsync(x => x.Id == id);
    return r is null
        ? Results.NotFound(new { error = $"Không tìm thấy yêu cầu {id}" })
        : Results.Ok(ReservationDto.From(r));
});

app.MapGet("/users/{userId:int:min(1)}/reservations", async (int userId, string? status, LibraryDbContext db) =>
{
    if (!await db.Users.AnyAsync(u => u.Id == userId))
        return Results.NotFound(new { error = $"Không tìm thấy người dùng {userId}" });

    var q = db.Reservations.AsNoTracking().Include(r => r.Book).Where(r => r.UserId == userId);
    if (!string.IsNullOrWhiteSpace(status))
    {
        if (!Enum.TryParse<ReservationStatus>(status, ignoreCase: true, out var st))
            return Results.BadRequest(new { error = "status phải là Pending|Reserved|Rejected|Borrowed|Returned" });
        q = q.Where(r => r.Status == st);
    }
    var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
    return Results.Ok(list.Select(r => ReservationDto.From(r)));
});

// ===== Mở rộng nhỏ: thủ thư xác nhận cho mượn / nhận trả sách (đồng bộ) =====
app.MapPost("/reservations/{id:guid}/borrow", async (Guid id, LibraryDbContext db) =>
{
    var r = await db.Reservations.Include(x => x.Book).FirstOrDefaultAsync(x => x.Id == id);
    if (r is null) return Results.NotFound(new { error = "Không tìm thấy yêu cầu" });
    if (r.Status != ReservationStatus.Reserved)
        return Results.Conflict(new { error = "Chỉ yêu cầu Reserved mới chuyển được sang Borrowed" });

    r.Status = ReservationStatus.Borrowed;
    r.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(ReservationDto.From(r));
});

app.MapPost("/reservations/{id:guid}/return", async (Guid id, LibraryDbContext db) =>
{
    var r = await db.Reservations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    if (r is null) return Results.NotFound(new { error = "Không tìm thấy yêu cầu" });

    await using var tx = await db.Database.BeginTransactionAsync();
    var changed = await db.Reservations
        .Where(x => x.Id == id && x.Status == ReservationStatus.Borrowed)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ReservationStatus.Returned)
                                  .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
    if (changed == 0)
        return Results.Conflict(new { error = "Chỉ yêu cầu Borrowed mới trả được" });

    await db.Books.Where(b => b.Id == r.BookId)
        .ExecuteUpdateAsync(s => s.SetProperty(b => b.AvailableCopies, b => b.AvailableCopies + 1));
    await tx.CommitAsync();
    return Results.Ok(new { id, status = "Returned" });
});

app.Run();

// Postgres trong Docker có thể chưa sẵn sàng ngay -> thử lại tối đa 10 lần
static async Task InitDatabaseAsync(WebApplication app)
{
    for (var i = 1; i <= 10; i++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LibraryDbContext>()
                .Database.EnsureCreatedAsync();
            app.Logger.LogInformation("Database ready");
            return;
        }
        catch (Exception ex) when (i < 10)
        {
            app.Logger.LogWarning("DB chưa sẵn sàng ({Msg}), thử lại {I}/10", ex.Message, i);
            await Task.Delay(3000);
        }
    }
}
