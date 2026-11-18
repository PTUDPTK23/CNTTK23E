namespace Library.Shared;

public enum ReservationStatus { Pending, Reserved, Rejected, Borrowed, Returned }

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public class LibraryUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Reservation
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public ReservationStatus Status { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Book? Book { get; set; }
}
