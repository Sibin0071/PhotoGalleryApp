using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using PhotoGalleryApp.Models;
using PhotoGalleryApp.Data;
using Microsoft.AspNetCore.Identity;

namespace PhotoGalleryApp.Pages
{
    [Authorize]
    public class UploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public UploadModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [BindProperty]
        public IFormFile Upload { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; } // ✅ optional for admin

        public async Task<IActionResult> OnPostAsync()
        {
            if (Upload == null)
                return Page();

            var fileName = Path.GetFileName(Upload.FileName);
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await Upload.CopyToAsync(fileStream);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var effectiveUserId = isAdmin && !string.IsNullOrEmpty(UserId)
                ? UserId
                : currentUser.Id;

            var newFile = new GalleryFile
            {
                FileName = fileName,
                FileUrl = $"/uploads/{fileName}",
                ContentType = Upload.ContentType,
                UploadedBy = currentUser?.Id ?? "",  // uploader = current user (admin or self)
                UserId = effectiveUserId,             // owner = impersonated user or self
                UploadedAt = DateTime.UtcNow
            };

            _context.GalleryFiles.Add(newFile);
            await _context.SaveChangesAsync();

            if (Upload.ContentType.StartsWith("video"))
            {
                return RedirectToPage("/GalleryVideo", new { UserId = effectiveUserId }); // ✅ Ensure UserId persists
            }
            else
            {
                return RedirectToPage("/GalleryImage", new { UserId = effectiveUserId }); // ✅ Ensure UserId persists
            }
        }
    }
}
