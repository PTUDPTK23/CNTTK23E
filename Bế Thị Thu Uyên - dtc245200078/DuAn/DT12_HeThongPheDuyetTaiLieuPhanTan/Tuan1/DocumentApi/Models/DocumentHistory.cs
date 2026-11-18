namespace DocumentApi.Models
{
    public class DocumentHistory
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string Action { get; set; } = string.Empty; // Created, Submitted, Approved, Rejected
        public string Actor { get; set; } = string.Empty; // Người thực hiện bước này
        public string? Note { get; set; } // Ghi chú hoặc lý do từ chối
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}