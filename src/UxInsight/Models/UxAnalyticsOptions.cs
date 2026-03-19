namespace UxInsight.Models;

public class UxAnalyticsOptions
{
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = "claude-sonnet-4-20250514";
    public bool Enabled { get; set; } = true;
}
