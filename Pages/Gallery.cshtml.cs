using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace PhotoGalleryApp.Pages
{
    public class GalleryModel : PageModel
    {
        public List<(string Url, string FileName)> MediaFiles { get; set; } = new();

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

            MediaFiles.Clear();

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                MediaFiles.Add((sasUri.ToString(), blobItem.Name));
            }
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

    public static class BlobClientExtensions
    {
        public static Uri GenerateSasUri(this BlobClient blobClient, BlobSasPermissions permissions, DateTimeOffset expiresOn)
        {
            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException("SAS generation not permitted.");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = expiresOn
            };
            sasBuilder.SetPermissions(permissions);

            return blobClient.GenerateSasUri(sasBuilder);
        }
    }
}
