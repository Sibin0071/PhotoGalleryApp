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
    public class GalleryImageModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // FINAL image URLs (CDN)
        public List<(string Url, string FileName)> ImageFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        public int TotalPages { get; set; }
        public string EffectiveUserId { get; set; } = "";

        // CDN base URL
        public string MediaBaseUrl { get; set; } = string.Empty;

        public GalleryImageModel(
            IConfiguration configuration,
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _configuration = configuration;
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var allowedEmails = new[] { "kg@gmail.com", "liza@gmail.com" };
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var isPrivilegedUser = isAdmin || allowedEmails.Contains(currentUser.Email);

            EffectiveUserId = UserId ?? currentUser.Id;

            // ✅ Load CDN base URL
            MediaBaseUrl = _configuration["MediaSettings:BaseUrl"]!.TrimEnd('/');

            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

            // ✅ STEP 1: Load from DB (SQL-safe only)
            var allMedia = await _db.GalleryFiles
                .Where(f => isPrivilegedUser || f.UserId == currentUser.Id)
                .Where(f => f.UserId == EffectiveUserId)
                .OrderByDescending(f => f.UploadedAt)
                .AsNoTracking()
                .ToListAsync();

            // ✅ STEP 2: Filter extensions IN MEMORY (EF-safe)
            var imageMedia = allMedia
                .Where(f =>
                {
                    var ext = Path.GetExtension(f.FileName);
                    return !string.IsNullOrEmpty(ext) &&
                           validExtensions.Contains(ext.ToLowerInvariant());
                })
                .ToList();

            // ✅ STEP 3: BUILD CDN URLs (NO SAS)
            var allImageFiles = imageMedia
                .Select(m => (
                    Url: $"{MediaBaseUrl}/{m.FileName}",
                    FileName: m.FileName
                ))
                .ToList();

            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allImageFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages));

            ImageFiles = allImageFiles
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        // 🔒 DELETE (Blob + DB)
        public async Task<IActionResult> OnPostDeleteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var allowedEmails = new[] { "kg@gmail.com", "liza@gmail.com" };
            var isPrivilegedUser =
                await _userManager.IsInRoleAsync(currentUser, "Admin") ||
                allowedEmails.Contains(currentUser.Email);

            var record = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (record != null)
            {
                if (!isPrivilegedUser && record.UserId != currentUser.Id)
                    return Forbid();

                var containerClient = new BlobServiceClient(
                    _configuration.GetConnectionString("AzureBlobStorage"))
                    .GetBlobContainerClient("media");

                await containerClient.GetBlobClient(fileName).DeleteIfExistsAsync();

                _db.GalleryFiles.Remove(record);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { PageNumber, UserId });
        }

        // 🔒 DOWNLOAD (SAS – correct)
        public async Task<IActionResult> OnPostDownload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("Filename is missing.");

            var currentUser = await _userManager.GetUserAsync(User);
            var allowedEmails = new[] { "kg@gmail.com", "liza@gmail.com" };
            var isPrivilegedUser =
                await _userManager.IsInRoleAsync(currentUser, "Admin") ||
                allowedEmails.Contains(currentUser.Email);

            var record = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (record != null && !isPrivilegedUser && record.UserId != currentUser.Id)
                return Forbid();

            var blobClient = new BlobServiceClient(
                _configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media")
                .GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30),
                ContentDisposition = $"attachment; filename={fileName}"
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return Redirect(blobClient.GenerateSasUri(sasBuilder).ToString());
        }
    }
}
