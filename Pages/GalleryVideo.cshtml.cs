using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Identity;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs.Models;

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
        public string? UserId { get; set; } // ✅ Support for admin override

        public int TotalPages { get; set; }
        public string EffectiveUserId { get; set; } = "";
        public string CurrentUserEmail { get; set; } = string.Empty;
        public bool CanModifyFiles { get; set; } = false;

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
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var effectiveUserId = UserId ?? currentUser.Id;
            EffectiveUserId = effectiveUserId;
            CurrentUserEmail = currentUser.Email ?? "";
            CanModifyFiles = isAdmin ||
                             (CurrentUserEmail == "kg@gmail.com" || CurrentUserEmail == "liza@gmail.com") &&
                             currentUser.Id == effectiveUserId;

            var allMedia = await _db.GalleryFiles
                .Where(f => isAdmin || f.UserId == currentUser.Id)
                .Where(f => f.UserId == effectiveUserId)
                .ToListAsync();

            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".3gp", ".avi", ".mkv" };
            var videoMedia = allMedia
                .Where(f => videoExtensions.Contains(Path.GetExtension(f.FileName).ToLower()))
                .ToList();

            var containerClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                                    .GetBlobContainerClient("media");

            var allVideoFiles = new List<(string Url, string FileName)>();

            foreach (var media in videoMedia)
            {
                var blobClient = containerClient.GetBlobClient(media.FileName);
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                allVideoFiles.Add((sasUri.ToString(), media.FileName));
            }

            if (isAdmin && string.IsNullOrEmpty(UserId))
            {
                var knownFiles = videoMedia.Select(f => f.FileName).ToHashSet();

                await foreach (BlobItem blob in containerClient.GetBlobsAsync())
                {
                    var ext = Path.GetExtension(blob.Name).ToLower();
                    if (videoExtensions.Contains(ext) && !knownFiles.Contains(blob.Name))
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                        allVideoFiles.Add((sasUri.ToString(), blob.Name));
                    }
                }
            }

            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allVideoFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages));

            VideoFiles = allVideoFiles
                .OrderByDescending(f =>
                    videoMedia.FirstOrDefault(m => m.FileName == f.FileName)?.UploadedAt
                    ?? DateTime.MinValue)
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<IActionResult> OnPostDownloadAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var email = currentUser.Email ?? "";

            var file = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (file == null) return NotFound();

            if (!(isAdmin || ((email == "kg@gmail.com" || email == "liza@gmail.com") && file.UserId == currentUser.Id)))
                return Forbid();

            var blobClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media")
                .GetBlobClient(fileName);

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));
            return Redirect(sasUri.ToString());
        }

        public async Task<IActionResult> OnPostDeleteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var email = currentUser.Email ?? "";

            var file = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (file == null) return NotFound();

            if (!(isAdmin || ((email == "kg@gmail.com" || email == "liza@gmail.com") && file.UserId == currentUser.Id)))
                return Forbid();

            var containerClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media");
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync();
            _db.GalleryFiles.Remove(file);
            await _db.SaveChangesAsync();

            return RedirectToPage(new { PageNumber, UserId });
        }
    }
}
