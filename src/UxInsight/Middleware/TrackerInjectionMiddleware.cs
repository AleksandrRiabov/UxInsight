using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using UxInsight.Models;

namespace UxInsight.Middleware;

public class TrackerInjectionMiddleware(RequestDelegate next, IOptions<UxAnalyticsOptions> options)
{
    private const string TrackerScript = "\n<script src=\"/ux-analytics/tracker.js\" defer></script>\n";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!options.Value.Enabled)
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        if (path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.'))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await next(context);

        memoryStream.Position = 0;

        if (context.Response.ContentType?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            var html = await new StreamReader(memoryStream).ReadToEndAsync();
            var closingBodyIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

            if (closingBodyIndex >= 0)
            {
                html = html.Insert(closingBodyIndex, TrackerScript);
            }

            var bytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength = bytes.Length;
            await originalBody.WriteAsync(bytes);
        }
        else
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(originalBody);
        }

        context.Response.Body = originalBody;
    }
}
