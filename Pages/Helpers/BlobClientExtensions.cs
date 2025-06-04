using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System;

namespace PhotoGalleryApp.Pages.Helpers
{
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
