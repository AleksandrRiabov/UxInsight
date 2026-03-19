# UxInsight.Umbraco

AI-powered UX analytics for Umbraco v17.

Automatically tracks user behavior on your frontend pages and uses Claude AI to analyze the data and provide actionable UX improvement suggestions.

## Installation

```bash
dotnet add package UxInsight.Umbraco
```

## Setup

### 1. Ensure MapControllers is enabled in Program.cs

In your `WithEndpoints` section, make sure `MapControllers()` is called:

```csharp
app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
        u.EndpointRouteBuilder.MapControllers(); // Required for UxInsight API
    });
```

### 2. Add configuration to appsettings.json

```json
{
  "UxAnalytics": {
    "Enabled": true,
    "ClaudeApiKey": "YOUR-CLAUDE-API-KEY",
    "ClaudeModel": "claude-sonnet-4-20250514"
  }
}
```

Get your API key at [console.anthropic.com](https://console.anthropic.com).

### 3. Use it

1. Browse your frontend pages to collect tracking data
2. Go to **Umbraco Backoffice > Content section > UX Analytics** tab
3. Click **Run AI Analysis** to get AI-powered insights

That's it! The tracker script is automatically injected into all frontend pages and the backoffice dashboard is registered automatically.

## What it tracks

- **Page views** and navigation paths between pages
- **Scroll depth** per page (how far users scroll)
- **Click patterns** (which elements get clicked and how often)
- **Time on page** (average time spent per page)
- **Form interactions** (which fields get focus and which are abandoned)
- **Mouse movement** heatmap data (10x10 grid)

## What the AI analyzes

- **Conversion Bottlenecks** - where users drop off, with severity ratings
- **UX Suggestions** - actionable improvements with impact/effort ratings
- **Stuck Points** - pages where users struggle and why
- **Heatmap Insights** - what mouse and click patterns reveal

## How it works

1. A lightweight JavaScript tracker (~4KB) is automatically injected into all frontend pages
2. User behavior events are batched and sent to `/api/ux-analytics/track` every 5 seconds
3. Data is stored in a separate SQLite database (`UxAnalytics.sqlite.db`)
4. When you click "Run AI Analysis", the data is aggregated and sent to Claude AI
5. Claude returns structured insights displayed in the backoffice dashboard

## Configuration options

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Enable/disable tracking |
| `ClaudeApiKey` | `""` | Your Anthropic API key |
| `ClaudeModel` | `claude-sonnet-4-20250514` | Claude model to use for analysis |

## Requirements

- Umbraco v17+
- .NET 10
- Claude API key from [Anthropic](https://console.anthropic.com)

## License

MIT
