using System;
using System.ComponentModel.DataAnnotations;

namespace PhotoGalleryApp.Models
{
    public class UserLoginHistory
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime LoginTime { get; set; }

        public UserLoginHistory()
        {
            // Set IST as the default time zone
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            LoginTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone);
        }
    }
}
