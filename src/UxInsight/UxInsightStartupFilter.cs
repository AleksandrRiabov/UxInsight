using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using UxInsight.Middleware;

namespace UxInsight;

public class UxInsightStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<TrackerInjectionMiddleware>();
            next(app);
        };
    }
}
