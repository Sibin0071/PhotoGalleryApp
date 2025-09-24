using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoGalleryApp.Models;
using Microsoft.AspNetCore.Identity;

namespace PhotoGalleryApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<GalleryFile> GalleryFiles { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
    }
}
