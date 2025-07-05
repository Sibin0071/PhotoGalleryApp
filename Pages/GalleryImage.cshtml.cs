using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace PhotoGalleryApp.Pages
{
    public class GalleryImageModel : PageModel
    {
        private readonly IConfiguration _configuration;
        public List<(string Url, string FileName)> ImageFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }

        public GalleryImageModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnGetAsync()
        {
            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerClient = new BlobServiceClient(connectionString).GetBlobContainerClient("media");

            var allImageFiles = new List<(string Url, string FileName)>();
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var extension = Path.GetExtension(blobItem.Name).ToLower();
                if (validExtensions.Contains(extension))
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                    allImageFiles.Add((sasUri.ToString(), blobItem.Name));
                }
            }

            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allImageFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages)); // Safety bounds

            ImageFiles = allImageFiles
                .Skip((PageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
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

        public IActionResult OnPostDownload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var blobClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                             .GetBlobContainerClient("media")
                             .GetBlobClient(fileName);

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(30));

            // Force download behavior by setting content-disposition header via redirect workaround
            var downloadUrl = sasUri.ToString() + "&rscd=attachment%3Bfilename%3D" + Uri.EscapeDataString(fileName);

            return Redirect(downloadUrl);
        }

    }
}
