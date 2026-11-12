var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Du lieu mau trong bo nho (Vung 2)
var books = new List<BookDto> {
    new(1, "Clean Code", "Robert C. Martin", 2008),
    new(2, "Designing Data-Intensive Applications", "Martin Kleppmann", 2017),
    new(3, "Phat trien ung dung phan tan", "Bo mon HTTT", 2025)
};

// Vung 3: Cac endpoint
// GET / : kiem tra may chu dang chay
app.MapGet("/", () => Results.Ok(new {
    service = "Library.Api",
    status = "running"
}));

// GET /api/books : danh sach sach
app.MapGet("/api/books", () => Results.Ok(books));

// GET /api/books/{id:int} : chi tiet mot cuon sach
app.MapGet("/api/books/{id:int}", (int id) => {
    var book = books.FirstOrDefault(b => b.Id == id);
    return book is null
        ? Results.NotFound(new ApiError("BOOK_NOT_FOUND", "Khong tim thay sach."))
        : Results.Ok(book);
});

// POST /api/books : them sach moi tu body JSON
app.MapPost("/api/books", (CreateBookRequest request) => {
    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Author) ||
        request.PublishYear <= 0) {
        return Results.BadRequest(new ApiError(
            "INVALID_BOOK",
            "Ten sach, tac gia khong duoc de trong va nam xuat ban phai lon hon 0."));
    }

    var duplicated = books.Any(b =>
        b.Title.Equals(request.Title.Trim(), StringComparison.OrdinalIgnoreCase));
    if (duplicated) {
        return Results.Conflict(new ApiError(
            "DUPLICATE_BOOK",
            "Sach voi tieu de nay da ton tai."));
    }

    var nextId = books.Count == 0 ? 1 : books.Max(b => b.Id) + 1;
    var book = new BookDto(
        Id: nextId,
        Title: request.Title.Trim(),
        Author: request.Author.Trim(),
        PublishYear: request.PublishYear);
    books.Add(book);
    return Results.Created($"/api/books/{book.Id}", book);
});

// Vung 4: Chay may chu
app.Run();

// Vung 5: Khai bao DTO (2 DTO + ApiError)
record BookDto(int Id, string Title, string Author, int PublishYear);
record CreateBookRequest(string? Title, string? Author, int PublishYear);
record ApiError(string Code, string Message);
