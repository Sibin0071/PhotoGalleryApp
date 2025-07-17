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
        public int TotalPages { get; set; }

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
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // Step 1: Load from DB based on role
            var allMedia = await _db.GalleryFiles
                .Where(f => isAdmin || f.UserId == user.Id)
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

            // Step 2: Add orphan (non-DB) blobs for Admin
            if (isAdmin)
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

            // Step 3: Pagination
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allVideoFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages));

            VideoFiles = allVideoFiles
                .OrderByDescending(f => f.FileName) // Optional: change sort
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public IActionResult OnPostDownload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var blobClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media")
                .GetBlobClient(fileName);

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));

            return Redirect(sasUri.ToString());
        }

        public async Task<IActionResult> OnPostDeleteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var containerClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media");

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();

            // Also remove from DB if exists
            var fileRecord = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (fileRecord != null)
            {
                _db.GalleryFiles.Remove(fileRecord);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { PageNumber });
        }
    }
}
