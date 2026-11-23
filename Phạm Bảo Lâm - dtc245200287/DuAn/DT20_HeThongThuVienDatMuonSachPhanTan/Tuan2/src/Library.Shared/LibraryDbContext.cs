using Microsoft.EntityFrameworkCore;

namespace Library.Shared;

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<LibraryUser> Users => Set<LibraryUser>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Reservation>(e =>
        {
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(r => r.UserId);
            e.HasOne(r => r.Book).WithMany().HasForeignKey(r => r.BookId);
        });

        // Dữ liệu mẫu: EnsureCreated sẽ tự chèn khi tạo database
        b.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Clean Code", Author = "Robert C. Martin", TotalCopies = 3, AvailableCopies = 3 },
            new Book { Id = 2, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", TotalCopies = 1, AvailableCopies = 1 },
            new Book { Id = 3, Title = "Nhà giả kim", Author = "Paulo Coelho", TotalCopies = 2, AvailableCopies = 2 },
            new Book { Id = 4, Title = "Dế Mèn phiêu lưu ký", Author = "Tô Hoài", TotalCopies = 1, AvailableCopies = 1 });

        b.Entity<LibraryUser>().HasData(
            new LibraryUser { Id = 1, Name = "Nguyễn Văn A" },
            new LibraryUser { Id = 2, Name = "Trần Thị B" },
            new LibraryUser { Id = 3, Name = "Lê Văn C" });
    }
}
