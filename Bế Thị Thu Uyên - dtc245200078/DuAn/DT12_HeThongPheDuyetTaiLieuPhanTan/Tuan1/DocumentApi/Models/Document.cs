namespace DocumentApi.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty; // Người tạo
        public string? Approver { get; set; } // Người phê duyệt
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected[cite: 2]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}