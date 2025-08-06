using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PhotoGalleryApp.Data;
using PhotoGalleryApp.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using PhotoGalleryApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using PhotoGalleryApp.UserIdProviders; // ✅ Custom UserIdProvider

var builder = WebApplication.CreateBuilder(args);

// ✅ Environment-based configuration
var azureBlobConn = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION");
if (!string.IsNullOrEmpty(azureBlobConn))
{
    builder.Configuration["ConnectionStrings:AzureBlobStorage"] = azureBlobConn;
}

var sqlConn = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION");
if (!string.IsNullOrEmpty(sqlConn))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = sqlConn;
}

// ✅ Database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// ✅ Identity with Roles and UI
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// ✅ Upload size limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5_368_709_120;
});
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 5_368_709_120;
});

// ✅ Razor Pages + route protection
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Identity/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Identity/Account/Register");
    options.Conventions.AllowAnonymousToPage("/Identity/Account/ForgotPassword");
    options.Conventions.AllowAnonymousToPage("/Identity/Account/ResetPassword");
});

builder.Services.AddControllers();

// ✅ SignalR with custom user identity provider
builder.Services.AddSignalR(); // or AddSignalR().AddAzureSignalR() if you're using Azure SignalR Service
builder.Services.AddSingleton<IUserIdProvider, EmailBasedUserIdProvider>(); // ✅ <-- THIS LINE IS NEW

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");

// ✅ API: Generate SAS URL with unique file name
app.MapPost("/api/generate-sas-url", async (HttpRequest request, IConfiguration config) =>
{
    var form = await request.ReadFormAsync();
    var fileName = form["fileName"].ToString();
    var contentType = form["contentType"].ToString();

    if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType))
        return Results.BadRequest(new { success = false, message = "Missing file name or content type." });

    var normalizedContentType = contentType.ToLower().Trim();
    var allowedTypes = new[]
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska", "video/webm",
        "application/octet-stream", "video/3gpp"
    };

    bool isLikelySafeMp4 = fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
        && (normalizedContentType == "application/octet-stream" || normalizedContentType == "video/3gpp");

    if (!allowedTypes.Contains(normalizedContentType) && !isLikelySafeMp4)
        return Results.BadRequest(new { success = false, message = $"Unsupported file type '{normalizedContentType}'." });

    var ext = Path.GetExtension(fileName);
    var nameOnly = Path.GetFileNameWithoutExtension(fileName);
    var uniqueFileName = $"{nameOnly}_{Guid.NewGuid():N}{ext}";

    var connectionString = config.GetConnectionString("AzureBlobStorage");
    var containerClient = new BlobContainerClient(connectionString, "media");
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(uniqueFileName);

    if (!blobClient.CanGenerateSasUri)
        return Results.Json(new { success = false, message = "Cannot generate SAS URL. Ensure the connection string uses Account Key." }, statusCode: 500);

    var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(30));
    return Results.Ok(new { success = true, sasUrl = sasUri.ToString(), uniqueFileName });
});

// ✅ API: Save media record to DB
app.MapPost("/api/save-media-record", async (
    HttpContext context,
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager) =>
{
    var media = await context.Request.ReadFromJsonAsync<GalleryFile>();
    if (media == null || string.IsNullOrWhiteSpace(media.FileName))
        return Results.BadRequest(new { success = false, message = "Invalid file data." });

    var currentUser = await userManager.GetUserAsync(context.User);
    if (currentUser == null)
        return Results.Unauthorized();

    var isAdmin = await userManager.IsInRoleAsync(currentUser, "Admin");

    media.UserId = isAdmin && !string.IsNullOrWhiteSpace(media.UserId)
        ? media.UserId
        : currentUser.Id;

    media.UploadedBy = currentUser.Email;
    media.UploadedAt = DateTime.UtcNow;

    db.GalleryFiles.Add(media);
    await db.SaveChangesAsync();

    return Results.Ok(new { success = true });
});

// ✅ DB Setup + Role + Admin assignment
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = ["Admin", "User"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "sibincs33@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();
