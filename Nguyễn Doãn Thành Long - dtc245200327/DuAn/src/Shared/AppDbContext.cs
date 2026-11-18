using Microsoft.EntityFrameworkCore;

namespace Shared;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignments");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Title).IsRequired();
            entity.Property(a => a.Description).IsRequired();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.FullName).IsRequired();
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.ToTable("Submissions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).UseIdentityByDefaultColumn();
            entity.Property(s => s.AssignmentId).IsRequired();
            entity.Property(s => s.StudentId).IsRequired();
            entity.Property(s => s.Content).IsRequired();
            entity.Property(s => s.SubmittedAt).IsRequired();
            entity.Property(s => s.Status).IsRequired();
        });
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Assignments.AnyAsync())
        {
            db.Assignments.AddRange(
                new Assignment
                {
                    Id = 1,
                    Title = "Bài tập 1: Xây dựng RESTful API",
                    Description = "Thiết kế và triển khai các API nộp bài và tra cứu kết quả."
                },
                new Assignment
                {
                    Id = 2,
                    Title = "Bài tập 2: Hàng đợi tin nhắn RabbitMQ",
                    Description = "Tích hợp RabbitMQ để chuyển yêu cầu chấm bài sang Grading Worker."
                },
                new Assignment
                {
                    Id = 3,
                    Title = "Bài tập 3: Thông báo thời gian thực SignalR",
                    Description = "Đẩy thông báo kết quả chấm bài realtime về Student Client."
                }
            );
        }

        if (!await db.Students.AnyAsync())
        {
            db.Students.AddRange(
                new Student { Id = "SV001", FullName = "Nguyễn Doãn Thành Long" },
                new Student { Id = "SV002", FullName = "Trần Thị Dung" },
                new Student { Id = "SV003", FullName = "Hoàng Công Chiến" }
            );
        }

        await db.SaveChangesAsync();
    }
}
