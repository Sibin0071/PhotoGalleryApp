using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace PhotoGalleryApp.Pages
{
    public class GalleryVideoModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public List<(string Url, string FileName)> VideoFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        public int TotalPages { get; set; }
        public string EffectiveUserId { get; set; } = "";
        public string CurrentUserEmail { get; set; } = string.Empty;
        public bool CanModifyFiles { get; set; } = false;

        public string MediaBaseUrl { get; set; } = string.Empty;

        public GalleryVideoModel(
            IConfiguration configuration,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser!, "Admin");

            EffectiveUserId = UserId ?? currentUser!.Id;
            CurrentUserEmail = currentUser.Email ?? "";

            CanModifyFiles =
                isAdmin ||
                ((CurrentUserEmail == "kg@gmail.com" || CurrentUserEmail == "liza@gmail.com")
                 && currentUser.Id == EffectiveUserId);

            // ✅ Load CDN base URL
            MediaBaseUrl = _configuration["MediaSettings:BaseUrl"]!.TrimEnd('/');

            // ✅ Load DB records FIRST (EF-safe)
            var allMedia = await _db.GalleryFiles
                .Where(f => isAdmin || f.UserId == currentUser.Id)
                .Where(f => f.UserId == EffectiveUserId)
                .ToListAsync();

            // ✅ Filter videos in memory
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".3gp", ".avi", ".mkv" };

            var videoMedia = allMedia
                .Where(f => videoExtensions.Contains(Path.GetExtension(f.FileName).ToLower()))
                .OrderByDescending(f => f.UploadedAt)
                .ToList();

            // ✅ Build CDN URLs (NO SAS)
            var allVideoFiles = videoMedia
                .Select(v => (
                    Url: $"{MediaBaseUrl}/{v.FileName}",
                    FileName: v.FileName
                ))
                .ToList();

            // ✅ Pagination
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allVideoFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages));

            VideoFiles = allVideoFiles
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // ================= DOWNLOAD (SAS – SECURE) =================
        public async Task<IActionResult> OnPostDownloadAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser!, "Admin");
            var email = currentUser!.Email ?? "";

            var file = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (file == null) return NotFound();

            if (!(isAdmin || ((email == "kg@gmail.com" || email == "liza@gmail.com") && file.UserId == currentUser.Id)))
                return Forbid();

            var blobClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media")
                .GetBlobClient(fileName);

            var sasUri = blobClient.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddMinutes(30));

            return Redirect(sasUri.ToString());
        }

        // ================= DELETE =================
        public async Task<IActionResult> OnPostDeleteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser!, "Admin");
            var email = currentUser!.Email ?? "";

            var file = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (file == null) return NotFound();

            if (!(isAdmin || ((email == "kg@gmail.com" || email == "liza@gmail.com") && file.UserId == currentUser.Id)))
                return Forbid();

            var containerClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media");

            await containerClient.GetBlobClient(fileName).DeleteIfExistsAsync();

            _db.GalleryFiles.Remove(file);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { PageNumber, UserId });
        }
    }
}
