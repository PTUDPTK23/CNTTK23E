using System;
using EventFlow.EventApi.Entities;
using EventFlow.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EventApi.Data;

public class EventFlowDbContext : DbContext
{
    public EventFlowDbContext(DbContextOptions<EventFlowDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
        });

        // Participant configuration
        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FullName).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(p => p.Email).IsUnique();
        });

        // Registration configuration
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RegistrationCode).IsRequired().HasMaxLength(50);
            entity.HasIndex(r => r.RegistrationCode).IsUnique();

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Registrations)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Participant)
                  .WithMany(p => p.Registrations)
                  .HasForeignKey(r => r.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.EventId, r.ParticipantId });
        });
    }

    public static async Task SeedAsync(EventFlowDbContext context)
    {
        if (await context.Events.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var sampleEvents = new List<Event>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Vietnam Cloud & AI Summit 2026",
                Description = "Hội nghị thượng đỉnh thường niên về kiến trúc điện toán đám mây phân tán, Microservices và Trí tuệ nhân tạo tạo sinh (GenAI) cho doanh nghiệp.",
                Location = "Trung tâm Hội nghị Quốc gia, Hà Nội",
                StartTime = now.AddDays(5).Date.AddHours(9),
                EndTime = now.AddDays(5).Date.AddHours(17),
                MaxCapacity = 150,
                RegisteredCount = 0,
                Status = EventStatus.Upcoming,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = ".NET 10 & Distributed Systems Workshop",
                Description = "Workshop thực chiến lập trình kiến trúc phân tán với .NET 10, ASP.NET Core Minimal API, RabbitMQ Topic Exchange và PostgreSQL.",
                Location = "Tòa nhà FPT Cầu Giấy, Số 10 Phạm Văn Bạch, Hà Nội",
                StartTime = now.AddDays(8).Date.AddHours(8).AddMinutes(30),
                EndTime = now.AddDays(8).Date.AddHours(12),
                MaxCapacity = 60,
                RegisteredCount = 0,
                Status = EventStatus.Upcoming,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Cyber Security & DevSecOps Expo",
                Description = "Triển lãm và diễn đàn an ninh mạng hàng đầu khu vực, quy tụ các chuyên gia bảo mật đám mây và kỹ sư bảo vệ hạ tầng trọng yếu.",
                Location = "Khách sạn JW Marriott, Số 8 Đỗ Đức Dục, Nam Từ Liêm, Hà Nội",
                StartTime = now.AddDays(14).Date.AddHours(13).AddMinutes(30),
                EndTime = now.AddDays(14).Date.AddHours(18),
                MaxCapacity = 200,
                RegisteredCount = 0,
                Status = EventStatus.Upcoming,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Frontend Engineering & Blazor Modern UI",
                Description = "Tọa đàm chuyên sâu về trải nghiệm người dùng SaaS hiện đại, thiết kế UI/UX tinh tế và ứng dụng Blazor Interactive Server.",
                Location = "Trường Đại học Công nghệ Thông tin & Truyền thông Thái Nguyên",
                StartTime = now.AddDays(20).Date.AddHours(9),
                EndTime = now.AddDays(20).Date.AddHours(11).AddMinutes(30),
                MaxCapacity = 100,
                RegisteredCount = 0,
                Status = EventStatus.Upcoming,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "High-Throughput Message Brokers: RabbitMQ in Action",
                Description = "Chuyên đề phân tích chuyên sâu về RabbitMQ topic exchange, Dead-Letter Queues, consumer reliability và xử lý hàng triệu message.",
                Location = "Trực tuyến qua Zoom & YouTube Live Stream",
                StartTime = now.AddDays(25).Date.AddHours(19).AddMinutes(30),
                EndTime = now.AddDays(25).Date.AddHours(21).AddMinutes(30),
                MaxCapacity = 500,
                RegisteredCount = 0,
                Status = EventStatus.Upcoming,
                CreatedAt = now
            }
        };

        context.Events.AddRange(sampleEvents);
        await context.SaveChangesAsync();
    }
}
