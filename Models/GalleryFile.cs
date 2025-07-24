using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoGalleryApp.Models
{
    public class GalleryFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FileUrl { get; set; } = string.Empty;

        [Required]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string ContentType { get; set; } = string.Empty;

        [Required] 
        public string UserId { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }
}
