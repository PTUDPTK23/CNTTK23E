using Microsoft.EntityFrameworkCore;
using DocumentApi.Models;

namespace DocumentApi.Data
{
    public class DocumentDbContext : DbContext
    {
        public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentHistory> DocumentHistories { get; set; }

        // Bảng phục vụ Transactional Outbox Pattern & Idempotent Consumer trong hệ phân tán
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<ProcessedEvent> ProcessedEvents { get; set; }
    }
}