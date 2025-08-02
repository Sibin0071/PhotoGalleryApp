using System;

namespace PhotoGalleryApp.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string FromUserEmail { get; set; } = string.Empty;
        public string ToUserEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
