var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Khoi tao du lieu mau trong bo nho (In-Memory Data Store)
object syncLock = new();

var books = new List<Book>
{
    new()
    {
        Id = 1,
        Title = "Designing Data-Intensive Applications",
        Author = "Martin Kleppmann",
        ISBN = "978-1449373320",
        Category = "Distributed Systems",
        Description = "The big ideas behind reliable, scalable, and maintainable systems.",
        TotalCopies = 5,
        AvailableCopies = 3
    },
    new()
    {
        Id = 2,
        Title = "Clean Architecture",
        Author = "Robert C. Martin",
        ISBN = "978-0134494166",
        Category = "Software Engineering",
        Description = "A Craftsman's Guide to Software Structure and Design.",
        TotalCopies = 4,
        AvailableCopies = 2
    },
    new()
    {
        Id = 3,
        Title = "Building Microservices",
        Author = "Sam Newman",
        ISBN = "978-1492034025",
        Category = "Microservices",
        Description = "Designing Fine-Grained Systems.",
        TotalCopies = 3,
        AvailableCopies = 0 // Dung de kiem thu loi het ban kha dung (BOOK_UNAVAILABLE)
    },
    new()
    {
        Id = 4,
        Title = "Enterprise Integration Patterns",
        Author = "Gregor Hohpe",
        ISBN = "978-0321200686",
        Category = "Messaging",
        Description = "Designing, building, and deploying messaging solutions.",
        TotalCopies = 6,
        AvailableCopies = 4
    },
    new()
    {
        Id = 5,
        Title = "Domain-Driven Design",
        Author = "Eric Evans",
        ISBN = "978-0321125217",
        Category = "Software Architecture",
        Description = "Tackling Complexity in the Heart of Software.",
        TotalCopies = 2,
        AvailableCopies = 1
    }
};

var reservations = new List<ReservationDto>();

// ====== ENDPOINTS ======

// 0. Kiem tra trang thai he thong
app.MapGet("/", () => Results.Ok(new
{
    service = "Library.Api",
    status = "running",
    topic = "De tai 20 - He thong thu vien va dat muon sach phan tan",
    version = "1.0.0"
}));

// 1. GET /books : Tra cuu danh sach tat ca sach
app.MapGet("/books", () =>
{
    lock (syncLock)
    {
        var result = books.Select(b => b.ToDto()).ToList();
        return Results.Ok(result);
    }
});

// 2. GET /books/{id} : Tra cuu thong tin chi tiet mot cuon sach
app.MapGet("/books/{id:int}", (int id) =>
{
    lock (syncLock)
    {
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book is null)
        {
            return Results.NotFound(new ApiError(
                "BOOK_NOT_FOUND",
                "Khong tim thay sach."));
        }

        return Results.Ok(book.ToDto());
    }
});

// 3a. POST /books/{bookId}/reservations : Gui yeu cau dat muon sach (BookId tu URI)
app.MapPost("/books/{bookId:int}/reservations", (int bookId, CreateReservationRequest request) =>
{
    return ProcessReservation(bookId, request.UserId, request.UserName);
});

// 3b. POST /reservations : Gui yeu cau dat muon sach (BookId tu Request Body theo tong quan De tai 20)
app.MapPost("/reservations", (CreateReservationBodyRequest request) =>
{
    return ProcessReservation(request.BookId, request.UserId, request.UserName);
});

IResult ProcessReservation(int bookId, string? userId, string? userName)
{
    lock (syncLock)
    {
        // Kiem tra thu tu 1: Sach co ton tai khong?
        var book = books.FirstOrDefault(b => b.Id == bookId);
        if (book is null)
        {
            return Results.NotFound(new ApiError(
                "BOOK_NOT_FOUND",
                "Khong tim thay sach."));
        }

        // Kiem tra thu tu 2: Thong tin nguoi dung co hop le khong?
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(userName))
        {
            return Results.BadRequest(new ApiError(
                "INVALID_RESERVATION",
                "Thong tin nguoi dung khong hop le."));
        }

        // Kiem tra thu tu 3: Sach con ban kha dung khong?
        if (book.AvailableCopies <= 0)
        {
            return Results.BadRequest(new ApiError(
                "BOOK_UNAVAILABLE",
                "Sach da het ban kha dung de dat muon."));
        }

        // Tao yeu cau dat muon moi
        book.AvailableCopies--; // Cap nhat giam so ban kha dung trong bo nho

        var reservation = new ReservationDto(
            Id: Guid.NewGuid(),
            BookId: bookId,
            UserId: userId.Trim(),
            UserName: userName.Trim(),
            Status: ReservationStatus.Pending,
            RequestedAt: DateTimeOffset.UtcNow
        );

        reservations.Add(reservation);

        // Tra ve HTTP 201 Created voi header Location
        return Results.Created($"/reservations/{reservation.Id}", reservation);
    }
}

// 4. GET /reservations/{id} : Tra cuu yeu cau dat muon theo Guid
app.MapGet("/reservations/{id:guid}", (Guid id) =>
{
    lock (syncLock)
    {
        var reservation = reservations.FirstOrDefault(r => r.Id == id);
        if (reservation is null)
        {
            return Results.NotFound(new ApiError(
                "RESERVATION_NOT_FOUND",
                "Khong tim thay yeu cau dat muon."));
        }

        return Results.Ok(reservation);
    }
});

// 5. GET /users/{userId}/reservations : Lay danh sach yeu cau dat muon cua mot nguoi dung
app.MapGet("/users/{userId}/reservations", (string userId) =>
{
    lock (syncLock)
    {
        var userReservations = reservations
            .Where(r => string.Equals(r.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Results.Ok(userReservations);
    }
});

app.Run();

// ====== MODELS & DTOS ======
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    public BookDto ToDto() => new(Id, Title, Author, ISBN, Category, Description, TotalCopies, AvailableCopies);
}

public record BookDto(
    int Id,
    string Title,
    string Author,
    string ISBN,
    string Category,
    string Description,
    int TotalCopies,
    int AvailableCopies
);

public record ReservationDto(
    Guid Id,
    int BookId,
    string UserId,
    string UserName,
    string Status,
    DateTimeOffset RequestedAt
);

public record CreateReservationRequest(
    string? UserId,
    string? UserName
);

public record CreateReservationBodyRequest(
    int BookId,
    string? UserId,
    string? UserName
);

public record ApiError(
    string Code,
    string Message
);

public static class ReservationStatus
{
    public const string Pending = "Pending";
    public const string Reserved = "Reserved";
    public const string Rejected = "Rejected";
    public const string Borrowed = "Borrowed";
    public const string Returned = "Returned";
}
