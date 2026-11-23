using Library.Shared;

namespace Library.Api;

public record BookDto(int Id, string Title, string Author, int TotalCopies, int AvailableCopies)
{
    public static BookDto From(Book b) => new(b.Id, b.Title, b.Author, b.TotalCopies, b.AvailableCopies);
}

public record CreateReservationRequest(int UserId, int BookId);

public record ReservationDto(Guid Id, int UserId, int BookId, string? BookTitle,
    string Status, string? Reason, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static ReservationDto From(Reservation r, string? title = null) =>
        new(r.Id, r.UserId, r.BookId, title ?? r.Book?.Title, r.Status.ToString(), r.Reason, r.CreatedAt, r.UpdatedAt);
}
