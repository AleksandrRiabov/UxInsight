using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UxInsight.Data;
using UxInsight.Models;

namespace UxInsight.Services;

public class ClaudeAnalysisService(
    ITrackingService trackingService,
    IHttpClientFactory httpClientFactory,
    IOptions<UxAnalyticsOptions> options,
    UxAnalyticsDbContext db,
    ILogger<ClaudeAnalysisService> logger) : IAnalysisService
{
    public async Task<AnalysisResult> AnalyzeAsync(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var now = DateTimeOffset.UtcNow;
        var periodStart = from ?? now.AddDays(-7);
        var periodEnd = to ?? now;

        var summary = await trackingService.GetSummaryAsync(periodStart, periodEnd);

        if (summary.TotalEvents == 0)
        {
            return new AnalysisResult
            {
                CreatedAt = now,
                AnalysisPeriodStart = periodStart,
                AnalysisPeriodEnd = periodEnd,
                TotalSessions = 0,
                TotalEvents = 0,
                ConversionBottlenecks = "[]",
                UxSuggestions = "[{\"area\":\"No Data\",\"suggestion\":\"No tracking data collected yet. Visit the website to generate some behavior data first.\",\"impact\":\"N/A\",\"effort\":\"N/A\"}]",
                StuckPoints = "[]",
                HeatmapInsights = "[]",
                RawResponse = "No data to analyze"
            };
        }

        var prompt = BuildPrompt(summary);
        var aiResponse = await CallClaudeAsync(prompt);
        var result = ParseResponse(aiResponse, summary, periodStart, periodEnd);

        db.AnalysisResults.Add(result);
        await db.SaveChangesAsync();

        return result;
    }

    public async Task<AnalysisResult?> GetLatestAsync()
    {
        return await db.AnalysisResults
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AnalysisResult>> GetHistoryAsync(int take = 10)
    {
        return await db.AnalysisResults
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    private string BuildPrompt(TrackingDataSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a UX analytics expert. Analyze the following website user behavior data and provide actionable insights.");
        sb.AppendLine();
        sb.AppendLine($"DATA PERIOD: {summary.PeriodStart:yyyy-MM-dd HH:mm} to {summary.PeriodEnd:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"TOTAL SESSIONS: {summary.TotalSessions}");
        sb.AppendLine($"TOTAL EVENTS: {summary.TotalEvents}");
        sb.AppendLine();

        if (summary.TopPages.Count > 0)
        {
            sb.AppendLine("PAGE VIEWS (top pages):");
            foreach (var p in summary.TopPages)
                sb.AppendLine($"  - {p.PageUrl}: {p.Views} views, {p.UniqueSessions} unique sessions");
            sb.AppendLine();
        }

        if (summary.ScrollDepths.Count > 0)
        {
            sb.AppendLine("SCROLL DEPTH (average % scrolled per page):");
            foreach (var s in summary.ScrollDepths)
                sb.AppendLine($"  - {s.PageUrl}: {s.AverageScrollPercent:F1}%");
            sb.AppendLine();
        }

        if (summary.TopClicks.Count > 0)
        {
            sb.AppendLine("CLICK PATTERNS (most clicked elements):");
            foreach (var c in summary.TopClicks)
                sb.AppendLine($"  - {c.PageUrl} | {c.ElementSelector}: {c.ClickCount} clicks");
            sb.AppendLine();
        }

        if (summary.TimeOnPage.Count > 0)
        {
            sb.AppendLine("TIME ON PAGE (average seconds):");
            foreach (var t in summary.TimeOnPage)
                sb.AppendLine($"  - {t.PageUrl}: {t.AverageSeconds:F1}s");
            sb.AppendLine();
        }

        if (summary.FormInteractions.Count > 0)
        {
            sb.AppendLine("FORM INTERACTIONS (field focus and abandon patterns):");
            foreach (var f in summary.FormInteractions)
                sb.AppendLine($"  - {f.PageUrl} | {f.FieldSelector}: {f.FocusCount} focuses, {f.BlurWithoutSubmitCount} abandoned");
            sb.AppendLine();
        }

        if (summary.NavigationPaths.Count > 0)
        {
            sb.AppendLine("NAVIGATION PATHS (user flows between pages):");
            foreach (var n in summary.NavigationPaths)
                sb.AppendLine($"  - {n.FromPage} -> {n.ToPage}: {n.Count} transitions");
            sb.AppendLine();
        }

        sb.AppendLine("Respond with ONLY valid JSON (no markdown, no code blocks) using this exact structure:");
        sb.AppendLine("""
{
  "conversionBottlenecks": [
    {"issue": "description", "severity": "high|medium|low", "page": "/url", "recommendation": "what to do"}
  ],
  "uxSuggestions": [
    {"area": "area name", "suggestion": "what to improve", "impact": "high|medium|low", "effort": "high|medium|low"}
  ],
  "stuckPoints": [
    {"page": "/url", "indicator": "what metric shows this", "description": "why users are stuck"}
  ],
  "heatmapInsights": [
    {"page": "/url", "observation": "what the data shows", "recommendation": "what to change"}
  ]
}
""");

        return sb.ToString();
    }

    private async Task<string> CallClaudeAsync(string prompt)
    {
        var opts = options.Value;
        if (string.IsNullOrEmpty(opts.ClaudeApiKey))
        {
            logger.LogWarning("Claude API key not configured. Returning mock analysis.");
            return GetMockResponse();
        }

        var client = httpClientFactory.CreateClient("ClaudeApi");
        client.DefaultRequestHeaders.Add("x-api-key", opts.ClaudeApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var requestBody = JsonSerializer.Serialize(new
        {
            model = opts.ClaudeModel,
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, responseBody);
            return GetMockResponse();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content ?? GetMockResponse();
    }

    private AnalysisResult ParseResponse(string aiResponse, TrackingDataSummary summary, DateTimeOffset periodStart, DateTimeOffset periodEnd)
    {
        var result = new AnalysisResult
        {
            CreatedAt = DateTimeOffset.UtcNow,
            AnalysisPeriodStart = periodStart,
            AnalysisPeriodEnd = periodEnd,
            TotalSessions = summary.TotalSessions,
            TotalEvents = summary.TotalEvents,
            RawResponse = aiResponse
        };

        try
        {
            using var doc = JsonDocument.Parse(aiResponse);
            var root = doc.RootElement;

            result.ConversionBottlenecks = GetJsonArray(root, "conversionBottlenecks");
            result.UxSuggestions = GetJsonArray(root, "uxSuggestions");
            result.StuckPoints = GetJsonArray(root, "stuckPoints");
            result.HeatmapInsights = GetJsonArray(root, "heatmapInsights");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse Claude response as JSON");
            result.UxSuggestions = JsonSerializer.Serialize(new[]
            {
                new { area = "Raw Analysis", suggestion = aiResponse, impact = "N/A", effort = "N/A" }
            });
        }

        return result;
    }

    private static string GetJsonArray(JsonElement root, string property)
    {
        if (root.TryGetProperty(property, out var val))
            return val.GetRawText();
        return "[]";
    }

    private static string GetMockResponse() => """
    {
      "conversionBottlenecks": [
        {"issue": "API key not configured - showing sample analysis", "severity": "low", "page": "/", "recommendation": "Add your Claude API key in appsettings.json under UxAnalytics:ClaudeApiKey to get real AI analysis"}
      ],
      "uxSuggestions": [
        {"area": "Configuration", "suggestion": "Set up your Claude API key to enable AI-powered UX analysis of your tracked behavior data", "impact": "high", "effort": "low"}
      ],
      "stuckPoints": [
        {"page": "N/A", "indicator": "No AI analysis available", "description": "Configure the Claude API key to identify where users get stuck on your site"}
      ],
      "heatmapInsights": [
        {"page": "N/A", "observation": "Tracking data is being collected", "recommendation": "Once the API key is configured, run analysis again for AI-powered heatmap insights"}
      ]
    }
    """;
}
