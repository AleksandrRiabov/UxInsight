using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UxInsight.Data;
using UxInsight.Models;

namespace UxInsight.Services;

public class TrackingService(UxAnalyticsDbContext db) : ITrackingService
{
    public async Task StoreEventsAsync(TrackingPayload payload, string? userAgent)
    {
        var events = payload.Events.Select(e => new TrackingEvent
        {
            SessionId = payload.SessionId,
            EventType = e.EventType,
            PageUrl = e.PageUrl,
            Referrer = e.Referrer,
            ElementSelector = e.ElementSelector,
            Data = e.Data,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(e.Timestamp),
            UserAgent = userAgent,
            ScreenWidth = e.ScreenWidth,
            ScreenHeight = e.ScreenHeight
        }).ToList();

        db.TrackingEvents.AddRange(events);
        await db.SaveChangesAsync();
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        if (!await db.TrackingEvents.AnyAsync())
            return new DashboardStats();

        return new DashboardStats
        {
            TotalSessions = await db.TrackingEvents.Select(e => e.SessionId).Distinct().CountAsync(),
            TotalEvents = await db.TrackingEvents.CountAsync(),
            FirstEvent = await db.TrackingEvents.MinAsync(e => e.Timestamp),
            LastEvent = await db.TrackingEvents.MaxAsync(e => e.Timestamp)
        };
    }

    public async Task<TrackingDataSummary> GetSummaryAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var events = await db.TrackingEvents
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .ToListAsync();

        var summary = new TrackingDataSummary
        {
            TotalSessions = events.Select(e => e.SessionId).Distinct().Count(),
            TotalEvents = events.Count,
            PeriodStart = from,
            PeriodEnd = to
        };

        summary.TopPages = events
            .Where(e => e.EventType == "pageview")
            .GroupBy(e => e.PageUrl)
            .Select(g => new PageViewSummary
            {
                PageUrl = g.Key,
                Views = g.Count(),
                UniqueSessions = g.Select(e => e.SessionId).Distinct().Count()
            })
            .OrderByDescending(p => p.Views)
            .Take(20)
            .ToList();

        summary.ScrollDepths = events
            .Where(e => e.EventType == "scroll" && e.Data != null)
            .GroupBy(e => e.PageUrl)
            .Select(g => new ScrollDepthSummary
            {
                PageUrl = g.Key,
                AverageScrollPercent = g.Average(e => ParseDouble(e.Data, "maxScroll"))
            })
            .Where(s => s.AverageScrollPercent > 0)
            .OrderBy(s => s.AverageScrollPercent)
            .Take(20)
            .ToList();

        summary.TopClicks = events
            .Where(e => e.EventType == "click" && e.ElementSelector != null)
            .GroupBy(e => new { e.PageUrl, e.ElementSelector })
            .Select(g => new ClickSummary
            {
                PageUrl = g.Key.PageUrl,
                ElementSelector = g.Key.ElementSelector!,
                ClickCount = g.Count()
            })
            .OrderByDescending(c => c.ClickCount)
            .Take(30)
            .ToList();

        summary.TimeOnPage = events
            .Where(e => e.EventType == "timeonpage" && e.Data != null)
            .GroupBy(e => e.PageUrl)
            .Select(g => new TimeOnPageSummary
            {
                PageUrl = g.Key,
                AverageSeconds = g.Average(e => ParseDouble(e.Data, "seconds"))
            })
            .Where(t => t.AverageSeconds > 0)
            .OrderByDescending(t => t.AverageSeconds)
            .Take(20)
            .ToList();

        summary.FormInteractions = events
            .Where(e => (e.EventType == "formfocus" || e.EventType == "formblur") && e.ElementSelector != null)
            .GroupBy(e => new { e.PageUrl, e.ElementSelector })
            .Select(g => new FormInteractionSummary
            {
                PageUrl = g.Key.PageUrl,
                FieldSelector = g.Key.ElementSelector!,
                FocusCount = g.Count(e => e.EventType == "formfocus"),
                BlurWithoutSubmitCount = g.Count(e => e.EventType == "formblur")
            })
            .OrderByDescending(f => f.BlurWithoutSubmitCount)
            .Take(20)
            .ToList();

        summary.NavigationPaths = events
            .Where(e => e.EventType == "pageview" && e.Referrer != null && e.Referrer != "")
            .GroupBy(e => new { From = e.Referrer!, To = e.PageUrl })
            .Select(g => new NavigationPathSummary
            {
                FromPage = g.Key.From,
                ToPage = g.Key.To,
                Count = g.Count()
            })
            .OrderByDescending(n => n.Count)
            .Take(20)
            .ToList();

        return summary;
    }

    private static double ParseDouble(string? json, string key)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(key, out var val))
                return val.GetDouble();
        }
        catch { }
        return 0;
    }
}
