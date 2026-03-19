namespace UxInsight.Models;

public class TrackingEvent
{
    public long Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string? Referrer { get; set; }
    public string? ElementSelector { get; set; }
    public string? Data { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? UserAgent { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
}
