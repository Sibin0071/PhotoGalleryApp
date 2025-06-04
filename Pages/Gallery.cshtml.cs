using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using PhotoGalleryApp.Pages.Helpers;
using Microsoft.Extensions.Configuration;

namespace PhotoGalleryApp.Pages
{
    public class GalleryModel : PageModel
    {
        public List<(string Url, string FileName)> MediaFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }

        private readonly IConfiguration _configuration;

        public GalleryModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerName = "media";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var allFiles = new List<(string Url, string FileName)>();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                allFiles.Add((sasUri.ToString(), blobItem.Name));
            }

            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allFiles.Count / (double)pageSize);
            MediaFiles = allFiles
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient("media");
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();

            return RedirectToPage();
        }

        public IActionResult OnPostDownload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient("media");
            var blobClient = containerClient.GetBlobClient(fileName);
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));

            return Redirect(sasUri.ToString());
        }
    }

}
