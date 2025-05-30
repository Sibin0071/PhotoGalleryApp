using Microsoft.AspNetCore.Http.Features;

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

app.Run();
