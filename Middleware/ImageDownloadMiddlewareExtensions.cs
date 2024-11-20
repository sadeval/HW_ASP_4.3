using HW_ASP_4._3.Middleware;
using Microsoft.AspNetCore.Builder;

namespace HW_ASP_4._3.Middleware
{
    public static class ImageDownloadMiddlewareExtensions
    {
        public static IApplicationBuilder UseImageDownloadMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ImageDownloadMiddleware>();
        }
    }
}
