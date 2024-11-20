using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using HW_ASP_4._3.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseImageDownloadMiddleware();

app.MapPost("/upload", async (HttpRequest request, IWebHostEnvironment env, ILogger<Program> logger) =>
{
    if (!request.HasFormContentType)
    {
        logger.LogWarning("Invalid form content type.");
        return Results.BadRequest(new { error = "Invalid form data." });
    }

    var form = await request.ReadFormAsync();

    // Log all form keys
    logger.LogInformation("Received form keys: {Keys}", string.Join(", ", form.Keys));

    var file = form.Files["image"];

    if (file == null || file.Length == 0)
    {
        logger.LogWarning("No image provided.");
        return Results.BadRequest(new { error = "No image provided." });
    }

    logger.LogInformation("Received file: {FileName}, Size: {Size} bytes", file.FileName, file.Length);

    // Check file type
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
    var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
    {
        logger.LogWarning("Invalid image format: {Extension}", extension);
        return Results.BadRequest(new { error = "Invalid image format." });
    }

    if (!allowedMimeTypes.Contains(file.ContentType))
    {
        logger.LogWarning("Invalid image MIME type: {ContentType}", file.ContentType);
        return Results.BadRequest(new { error = "Invalid image MIME type." });
    }

    if (file.Length > 5 * 1024 * 1024)
    {
        logger.LogWarning("File size exceeds the limit: {Size} bytes", file.Length);
        return Results.BadRequest(new { error = "File size exceeds the limit of 5MB." });
    }

    // Generate a unique file name
    var uniqueFileName = $"{Guid.NewGuid()}{extension}";

    // Path to save the file
    var uploadsFolder = Path.Combine(env.WebRootPath, "images");
    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

    // Create a folder if it doesn't exist
    if (!Directory.Exists(uploadsFolder))
    {
        Directory.CreateDirectory(uploadsFolder);
        logger.LogInformation("Created folder: {FolderPath}", uploadsFolder);
    }

    // Save file to disk
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
        logger.LogInformation("Saved file to: {FilePath}", filePath);
    }

    // Generate URL to access image
    var imageUrl = $"{request.Scheme}://{request.Host}/images/{uniqueFileName}";
    logger.LogInformation("Image URL: {ImageUrl}", imageUrl);

    return Results.Ok(new { url = imageUrl });
})
.WithName("UploadImage")
.Accepts<IFormFile>("multipart/form-data")
.Produces(200)
.Produces(400);

app.Run();
