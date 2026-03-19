namespace UxInsight.Models;

public class AnalysisResult
{
    public long Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AnalysisPeriodStart { get; set; }
    public DateTimeOffset AnalysisPeriodEnd { get; set; }
    public int TotalSessions { get; set; }
    public int TotalEvents { get; set; }
    public string ConversionBottlenecks { get; set; } = "[]";
    public string UxSuggestions { get; set; } = "[]";
    public string StuckPoints { get; set; } = "[]";
    public string HeatmapInsights { get; set; } = "[]";
    public string RawResponse { get; set; } = string.Empty;
}
