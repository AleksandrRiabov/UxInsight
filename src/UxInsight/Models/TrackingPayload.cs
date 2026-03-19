namespace UxInsight.Models;

public class TrackingPayload
{
    public string SessionId { get; set; } = string.Empty;
    public List<TrackingEventDto> Events { get; set; } = [];
}

public class TrackingEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string? Referrer { get; set; }
    public string? ElementSelector { get; set; }
    public string? Data { get; set; }
    public long Timestamp { get; set; }
    public int? ScreenWidth { get; set; }
    public int? ScreenHeight { get; set; }
}
