using Microsoft.AspNetCore.Http.Features;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// ✅ Increase form upload limit to 5 GB
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_368_709_120; // 5 GB
});

// ✅ Increase Kestrel request body limit too
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 5_368_709_120; // 5 GB
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

// ✅ Minimal API endpoint for AJAX file upload
app.MapPost("/api/upload", async (HttpRequest request, IConfiguration config) =>
{
    var maxFileSizeBytes = 5L * 1024 * 1024 * 1024; // 5 GB
    var allowedTypes = new[] {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm"
    };

    var form = await request.ReadFormAsync();
    var files = form.Files;

    if (files.Count == 0)
        return Results.BadRequest(new { success = false, message = "No file selected." });

    var connectionString = config.GetConnectionString("AzureBlobStorage");
    var containerClient = new BlobContainerClient(connectionString, "media");
    await containerClient.CreateIfNotExistsAsync();

    foreach (var file in files)
    {
        if (file.Length > maxFileSizeBytes)
            return Results.BadRequest(new { success = false, message = $"File '{file.FileName}' exceeds 5 GB limit." });

        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return Results.BadRequest(new { success = false, message = $"File '{file.FileName}' is not a supported format." });

        var blobClient = containerClient.GetBlobClient(file.FileName);
        using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    return Results.Ok(new { success = true, message = "Upload successful!" });
});

app.Run();
