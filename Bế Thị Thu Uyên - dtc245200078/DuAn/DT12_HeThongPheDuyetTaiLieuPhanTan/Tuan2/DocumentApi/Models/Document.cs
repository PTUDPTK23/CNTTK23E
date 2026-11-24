using System.ComponentModel.DataAnnotations;

namespace DocumentApi.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty; // Người tạo
        public string? Approver { get; set; } // Người phê duyệt
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Kiểm soát đồng thời lạc quan (Optimistic Concurrency Control) trong hệ phân tán
        [ConcurrencyCheck]
        public int Version { get; set; } = 1;
    }
}