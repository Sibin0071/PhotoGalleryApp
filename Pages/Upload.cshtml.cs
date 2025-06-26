using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Storage.Blobs;

namespace PhotoGalleryApp.Pages
{
    public class UploadModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private const long MaxFileSizeBytes = 5L * 1024 * 1024 * 1024; // 5 GB

        public UploadModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [IgnoreAntiforgeryToken]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 5368709120)] // 5 GB
        public async Task<IActionResult> OnPostAjaxUploadAsync(List<IFormFile> files)

        {
            if (files == null || files.Count == 0)
            {
                return new JsonResult(new { success = false, message = "No file selected." });
            }

            var allowedTypes = new[]
            {
                "image/jpeg", "image/png", "image/gif", "image/webp",
                "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
            };

            var connectionString = _configuration.GetConnectionString("AzureBlobStorage");
            var containerClient = new BlobServiceClient(connectionString)
                                  .GetBlobContainerClient("media");

            await containerClient.CreateIfNotExistsAsync();

            foreach (var file in files)
            {
                if (file.Length > MaxFileSizeBytes)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"File '{file.FileName}' exceeds the 5 GB limit."
                    });
                }

                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"File '{file.FileName}' is not a supported format."
                    });
                }

                var blobClient = containerClient.GetBlobClient(file.FileName);
                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return new JsonResult(new { success = true, message = "Upload successful!" });
        }
    }
}
