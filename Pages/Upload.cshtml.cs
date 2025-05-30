using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace PhotoGalleryApp.Pages
{
    public class UploadModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly long _maxFileSizeBytes = 5L * 1024 * 1024 * 1024; // 5 GB

        public string StatusMessage { get; set; } = string.Empty;

        public UploadModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 5368709120)] // 5 GB
        public async Task<IActionResult> OnPostAsync(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                StatusMessage = "No file selected.";
                return Page();
            }

            var allowedTypes = new[] {
                "image/jpeg", "image/png", "image/gif", "image/webp",
                "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
            };

            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerName = "media";

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            foreach (var file in files)
            {
                if (file.Length > _maxFileSizeBytes)
                {
                    StatusMessage = $"File '{file.FileName}' exceeds 5 GB limit.";
                    return Page();
                }

                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    StatusMessage = $"File '{file.FileName}' is not a supported image or video format.";
                    return Page();
                }

                var blobClient = containerClient.GetBlobClient(file.FileName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
            }

            StatusMessage = "Upload successful!";
            return Page();
        }
    }
}
