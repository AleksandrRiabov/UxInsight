using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using UxInsight.Data;
using UxInsight.Models;
using UxInsight.Services;

namespace UxInsight.Composers;

public class UxAnalyticsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<UxAnalyticsOptions>()
            .BindConfiguration("UxAnalytics");

        builder.Services.AddDbContext<UxAnalyticsDbContext>();

        builder.Services.AddScoped<ITrackingService, TrackingService>();
        builder.Services.AddScoped<IAnalysisService, ClaudeAnalysisService>();

        builder.Services.AddHttpClient("ClaudeApi");

        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, UxAnalyticsDbInitializer>();
    }
}

public class UxAnalyticsDbInitializer(IServiceProvider serviceProvider)
    : INotificationHandler<UmbracoApplicationStartedNotification>
{
    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UxAnalyticsDbContext>();
        db.Database.EnsureCreated();
    }
}
