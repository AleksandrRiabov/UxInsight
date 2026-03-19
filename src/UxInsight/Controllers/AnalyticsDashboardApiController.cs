using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Authorization;
using UxInsight.Services;

namespace UxInsight.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
[Route("api/ux-analytics/dashboard")]
public class AnalyticsDashboardApiController(
    ITrackingService trackingService,
    IAnalysisService analysisService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var stats = await trackingService.GetStatsAsync();
        return Ok(stats);
    }

    [HttpGet("analysis/latest")]
    public async Task<IActionResult> GetLatestAnalysis()
    {
        var result = await analysisService.GetLatestAsync();
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost("analysis/run")]
    public async Task<IActionResult> RunAnalysis()
    {
        var result = await analysisService.AnalyzeAsync();
        return Ok(result);
    }

    [HttpGet("analysis/history")]
    public async Task<IActionResult> GetHistory([FromQuery] int take = 10)
    {
        var results = await analysisService.GetHistoryAsync(take);
        return Ok(results);
    }
}
