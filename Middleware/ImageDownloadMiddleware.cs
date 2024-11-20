using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace HW_ASP_4._3.Middleware
{
    public class ImageDownloadMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageDownloadMiddleware> _logger;

        public ImageDownloadMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ImageDownloadMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Equals("/download-images", System.StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Received request for /download-images");

                var query = context.Request.Query;
                if (!query.ContainsKey("files"))
                {
                    _logger.LogWarning("No 'files' parameter in query string.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "No files specified." });
                    return;
                }

                var filesParam = query["files"].ToString();
                if (string.IsNullOrWhiteSpace(filesParam))
                {
                    _logger.LogWarning("'files' parameter is empty.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "Files parameter is empty." });
                    return;
                }

                var fileNames = filesParam.Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                                          .Select(f => f.Trim())
                                          .ToList();

                if (!fileNames.Any())
                {
                    _logger.LogWarning("No valid file names provided.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "No valid file names provided." });
                    return;
                }

                var imagesFolder = Path.Combine(_env.WebRootPath, "images");

                if (!Directory.Exists(imagesFolder))
                {
                    _logger.LogError("Images folder does not exist.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new { error = "Images folder does not exist on the server." });
                    return;
                }

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var fileName in fileNames)
                        {
                            var filePath = Path.Combine(imagesFolder, fileName);

                            if (File.Exists(filePath))
                            {
                                _logger.LogInformation($"Adding file to archive: {fileName}");
                                archive.CreateEntryFromFile(filePath, fileName);
                            }
                            else
                            {
                                _logger.LogWarning($"File not found: {fileName}");
                            }
                        }
                    }

                    memoryStream.Position = 0;

                    context.Response.ContentType = "application/zip";
                    context.Response.Headers.Append("Content-Disposition", "attachment; filename=images.zip");

                    _logger.LogInformation("Sending ZIP archive to client.");
                    await memoryStream.CopyToAsync(context.Response.Body);
                }

                return;
            }

            await _next(context);
        }
    }
}
