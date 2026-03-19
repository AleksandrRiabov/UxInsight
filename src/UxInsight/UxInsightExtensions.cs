using Microsoft.AspNetCore.Builder;
using UxInsight.Middleware;

namespace UxInsight;

public static class UxInsightExtensions
{
    /// <summary>
    /// Adds the UxInsight tracker injection middleware.
    /// Call this BEFORE UseUmbraco().
    /// </summary>
    public static IApplicationBuilder UseUxInsight(this IApplicationBuilder app)
    {
        app.UseMiddleware<TrackerInjectionMiddleware>();
        return app;
    }
}
