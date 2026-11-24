using System.ComponentModel.DataAnnotations;

namespace DocumentApi.Models
{
    public class ProcessedEvent
    {
        [Key]
        public Guid EventId { get; set; }
        public string ConsumerNode { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}