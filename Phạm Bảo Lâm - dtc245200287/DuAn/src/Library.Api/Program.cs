using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new ConcurrentDictionary<int, Book>(new Dictionary<int, Book>
{
    [1] = new(1, "Clean Code", "Robert C. Martin", 3, 3),
    [2] = new(2, "Designing Data-Intensive Applications", "Martin Kleppmann", 1, 1),
    [3] = new(3, "Nhà giả kim", "Paulo Coelho", 2, 2),
    [4] = new(4, "Dế Mèn phiêu lưu ký", "Tô Hoài", 1, 1)
});
var reservations = new ConcurrentDictionary<Guid, Reservation>();
var syncLock = new object();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/books", (string? keyword) =>
{
    var query = books.Values.AsEnumerable();
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(b =>
            b.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            b.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
    return Results.Ok(query.OrderBy(b => b.Id).Select(BookDto.From));
});

app.MapGet("/books/{id:int:min(1)}", (int id) =>
    books.TryGetValue(id, out var book)
        ? Results.Ok(BookDto.From(book))
        : Results.NotFound(new { error = $"Không tìm thấy sách có id = {id}" }));

app.MapPost("/reservations", (CreateReservationRequest req) =>
{
    var errors = new Dictionary<string, string[]>();
    if (req.UserId <= 0) errors["userId"] = ["userId phải lớn hơn 0"];
    if (req.BookId <= 0) errors["bookId"] = ["bookId phải lớn hơn 0"];
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    lock (syncLock)
    {
        if (!books.TryGetValue(req.BookId, out var book))
            return Results.NotFound(new { error = $"Không tìm thấy sách {req.BookId}" });

        if (book.AvailableCopies <= 0)
            return Results.Conflict(new { error = "Sách đã hết bản khả dụng" });

        books[req.BookId] = book with { AvailableCopies = book.AvailableCopies - 1 };

        var now = DateTime.UtcNow;
        var item = new Reservation(
            Guid.NewGuid(), req.UserId, req.BookId, book.Title,
            "Reserved", now, now);
        reservations[item.Id] = item;

        return Results.Created($"/reservations/{item.Id}", ReservationDto.From(item));
    }
});

app.MapGet("/reservations/{id:guid}", (Guid id) =>
    reservations.TryGetValue(id, out var item)
        ? Results.Ok(ReservationDto.From(item))
        : Results.NotFound(new { error = $"Không tìm thấy yêu cầu {id}" }));

app.MapGet("/users/{userId:int:min(1)}/reservations", (int userId) =>
{
    var list = reservations.Values
        .Where(r => r.UserId == userId)
        .OrderByDescending(r => r.CreatedAt)
        .Select(ReservationDto.From);
    return Results.Ok(list);
});

app.MapPost("/reservations/{id:guid}/borrow", (Guid id) =>
{
    lock (syncLock)
    {
        if (!reservations.TryGetValue(id, out var item))
            return Results.NotFound(new { error = "Không tìm thấy yêu cầu" });

        if (item.Status != "Reserved")
            return Results.Conflict(new { error = "Chỉ yêu cầu ở trạng thái Reserved mới được mượn" });

        var updated = item with { Status = "Borrowed", UpdatedAt = DateTime.UtcNow };
        reservations[id] = updated;
        return Results.Ok(ReservationDto.From(updated));
    }
});

app.MapPost("/reservations/{id:guid}/return", (Guid id) =>
{
    lock (syncLock)
    {
        if (!reservations.TryGetValue(id, out var item))
            return Results.NotFound(new { error = "Không tìm thấy yêu cầu" });

        if (item.Status != "Borrowed")
            return Results.Conflict(new { error = "Chỉ yêu cầu ở trạng thái Borrowed mới được trả" });

        var updated = item with { Status = "Returned", UpdatedAt = DateTime.UtcNow };
        reservations[id] = updated;

        if (books.TryGetValue(item.BookId, out var book))
            books[item.BookId] = book with { AvailableCopies = book.AvailableCopies + 1 };

        return Results.Ok(ReservationDto.From(updated));
    }
});

app.Run();

record Book(int Id, string Title, string Author, int TotalCopies, int AvailableCopies);
record Reservation(Guid Id, int UserId, int BookId, string BookTitle, string Status, DateTime CreatedAt, DateTime UpdatedAt);

record BookDto(int Id, string Title, string Author, int TotalCopies, int AvailableCopies)
{
    public static BookDto From(Book b) => new(b.Id, b.Title, b.Author, b.TotalCopies, b.AvailableCopies);
}

record CreateReservationRequest(int UserId, int BookId);

record ReservationDto(Guid Id, int UserId, int BookId, string BookTitle, string Status, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static ReservationDto From(Reservation r) =>
        new(r.Id, r.UserId, r.BookId, r.BookTitle, r.Status, r.CreatedAt, r.UpdatedAt);
}
