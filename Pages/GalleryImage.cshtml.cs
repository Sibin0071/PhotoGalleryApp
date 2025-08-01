﻿using Microsoft.AspNetCore.Mvc;
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

        public List<(string Url, string FileName)> ImageFiles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; } // ✅ new: optional userId passed by admin

        public int TotalPages { get; set; }
        public string EffectiveUserId { get; set; } = "";

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
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // ✅ Step 1: Determine effective userId (admin override)
            var effectiveUserId = UserId ?? currentUser.Id;
            EffectiveUserId = effectiveUserId;

            // Step 2: Load from DB (filtered)
            var allMedia = await _db.GalleryFiles
                .Where(f => isAdmin || f.UserId == currentUser.Id)
                .Where(f => f.UserId == effectiveUserId) // ✅ enforce exact user match if userId is passed
                .ToListAsync();

            // Step 3: Filter image files
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var imageMedia = allMedia
                .Where(f => validExtensions.Contains(Path.GetExtension(f.FileName).ToLower()))
                .ToList();

            // Step 4: Build blob SAS URLs
            var containerClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                                    .GetBlobContainerClient("media");

            var allImageFiles = new List<(string Url, string FileName)>();

            foreach (var media in imageMedia)
            {
                var blobClient = containerClient.GetBlobClient(media.FileName);
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                allImageFiles.Add((sasUri.ToString(), media.FileName));
            }

            // Step 5: Add orphan images (admin only, no userId filter)
            if (isAdmin && string.IsNullOrEmpty(UserId))
            {
                var knownFileNames = imageMedia.Select(m => m.FileName).ToHashSet();

                await foreach (BlobItem blob in containerClient.GetBlobsAsync())
                {
                    var ext = Path.GetExtension(blob.Name).ToLower();
                    if (validExtensions.Contains(ext) && !knownFileNames.Contains(blob.Name))
                    {
                        var blobClient = containerClient.GetBlobClient(blob.Name);
                        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
                        allImageFiles.Add((sasUri.ToString(), blob.Name));
                    }
                }
            }

            // Step 6: Pagination
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(allImageFiles.Count / (double)pageSize);
            PageNumber = Math.Max(1, Math.Min(PageNumber, TotalPages));

            ImageFiles = allImageFiles
    .OrderByDescending(f =>
        imageMedia.FirstOrDefault(m => m.FileName == f.FileName)?.UploadedAt
        ?? DateTime.MinValue // For orphan files with no DB record
    )
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

            // Delete from database if exists
            var record = await _db.GalleryFiles.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (record != null)
            {
                _db.GalleryFiles.Remove(record);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage(new { PageNumber, UserId }); // ✅ Preserve userId when redirecting
        }

        public IActionResult OnPostDownload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return BadRequest("Filename is missing.");

            var blobClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"))
                .GetBlobContainerClient("media")
                .GetBlobClient(fileName);

            var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30),
                ContentDisposition = $"attachment; filename={fileName}"
            };

            sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return Redirect(sasUri.ToString());
        }
    }
}
