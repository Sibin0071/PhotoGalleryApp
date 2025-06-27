using Microsoft.AspNetCore.Http.Features;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

var builder = WebApplication.CreateBuilder(args);

// ✅ Inject Azure connection string from environment variable if available
var azureConn = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION");
if (!string.IsNullOrEmpty(azureConn))
{
    builder.Configuration["ConnectionStrings:AzureBlobStorage"] = azureConn;
}

// Increase form upload limit to 5 GB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_368_709_120;
});

// Increase Kestrel request body limit
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 5_368_709_120;
});

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

// Minimal API for direct blob upload via SAS URL
app.MapPost("/api/generate-sas-url", async (HttpRequest request, IConfiguration config) =>
{
    var form = await request.ReadFormAsync();
    var fileName = form["fileName"].ToString();
    var contentType = form["contentType"].ToString();

    if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
        return Results.BadRequest(new { success = false, message = "Missing file name or content type." });

    var allowedTypes = new[]
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
    };

    if (!allowedTypes.Contains(contentType.ToLower()))
        return Results.BadRequest(new { success = false, message = "Unsupported file type." });

    var connectionString = config.GetConnectionString("AzureBlobStorage");
    var containerClient = new BlobContainerClient(connectionString, "media");
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(fileName);

    if (!blobClient.CanGenerateSasUri)
        return Results.Json(new { success = false, message = "Cannot generate SAS URL. Ensure the connection string uses Account Key." }, statusCode: 500);

    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(30));

    return Results.Ok(new { success = true, sasUrl = sasUri.ToString() });
});

app.Run();
