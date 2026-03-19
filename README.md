# UxInsight

AI-powered UX analytics for Umbraco v17.

Automatically tracks user behavior on your frontend pages (clicks, scroll depth, time on page, form interactions, navigation paths, mouse heatmap data) and uses Claude AI to analyze the data and provide actionable UX improvement suggestions.

## Installation

```bash
dotnet add package UxInsight
```

## Setup

### 1. Add middleware to Program.cs

Add `app.UseUxInsight()` **before** `app.UseUmbraco()`:

```csharp
app.UseUxInsight();

app.UseUmbraco()
    .WithMiddleware(u => { ... })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
        u.EndpointRouteBuilder.MapControllers();
    });
```

### 2. Add configuration to appsettings.json

```json
{
  "UxAnalytics": {
    "Enabled": true,
    "ClaudeApiKey": "sk-ant-api03-YOUR-KEY-HERE",
    "ClaudeModel": "claude-sonnet-4-20250514"
  }
}
```

Get your API key at [console.anthropic.com](https://console.anthropic.com).

### 3. Use it

- Browse your frontend pages to collect tracking data
- Go to **Umbraco Backoffice > Content section > UX Analytics** tab
- Click **Run AI Analysis** to get AI-powered insights

## What it tracks

- Page views and navigation paths
- Scroll depth per page
- Click patterns (which elements get clicked)
- Time spent on each page
- Form field interactions (focus/abandon)
- Mouse movement heatmap (10x10 grid)

## What the AI analyzes

- **Conversion Bottlenecks** - where users drop off
- **UX Suggestions** - actionable improvements with impact/effort ratings
- **Stuck Points** - pages where users struggle
- **Heatmap Insights** - what the mouse/click patterns reveal

## Requirements

- Umbraco v17+
- .NET 10
- Claude API key (from Anthropic)
