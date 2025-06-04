using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace PhotoGalleryApp.Pages
{
    public class GalleryVideoModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<(string Url, string FileName)> VideoFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }

        public GalleryVideoModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient("media");

            string[] videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".3gp", ".avi", ".mkv" };
            var allVideos = new List<(string Url, string FileName)>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var extension = Path.GetExtension(blobItem.Name).ToLower();
                if (videoExtensions.Contains(extension))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                    allVideos.Add((sasUri.ToString(), blobItem.Name));
                }
            }

            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allVideos.Count / (double)pageSize);
            VideoFiles = allVideos
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

            return RedirectToPage(new { PageNumber });
        }
    }
}
