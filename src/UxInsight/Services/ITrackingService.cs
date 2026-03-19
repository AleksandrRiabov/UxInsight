using UxInsight.Models;

namespace UxInsight.Services;

public interface ITrackingService
{
    Task StoreEventsAsync(TrackingPayload payload, string? userAgent);
    Task<TrackingDataSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to);
    Task<DashboardStats> GetStatsAsync();
}

public class TrackingDataSummary
{
    public int TotalSessions { get; set; }
    public int TotalEvents { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public List<PageViewSummary> TopPages { get; set; } = [];
    public List<ScrollDepthSummary> ScrollDepths { get; set; } = [];
    public List<ClickSummary> TopClicks { get; set; } = [];
    public List<TimeOnPageSummary> TimeOnPage { get; set; } = [];
    public List<FormInteractionSummary> FormInteractions { get; set; } = [];
    public List<NavigationPathSummary> NavigationPaths { get; set; } = [];
}

public class PageViewSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public int Views { get; set; }
    public int UniqueSessions { get; set; }
}

public class ScrollDepthSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public double AverageScrollPercent { get; set; }
}

public class ClickSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public string ElementSelector { get; set; } = string.Empty;
    public int ClickCount { get; set; }
}

public class TimeOnPageSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public double AverageSeconds { get; set; }
}

public class FormInteractionSummary
{
    public string PageUrl { get; set; } = string.Empty;
    public string FieldSelector { get; set; } = string.Empty;
    public int FocusCount { get; set; }
    public int BlurWithoutSubmitCount { get; set; }
}

public class NavigationPathSummary
{
    public string FromPage { get; set; } = string.Empty;
    public string ToPage { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardStats
{
    public int TotalSessions { get; set; }
    public int TotalEvents { get; set; }
    public DateTimeOffset? FirstEvent { get; set; }
    public DateTimeOffset? LastEvent { get; set; }
}
