using Microsoft.AspNetCore.Mvc;
using UxInsight.Models;
using UxInsight.Services;

namespace UxInsight.Controllers;

[ApiController]
[Route("api/ux-analytics")]
public class TrackingApiController(ITrackingService trackingService) : ControllerBase
{
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackingPayload payload)
    {
        if (string.IsNullOrEmpty(payload.SessionId) || payload.Events.Count == 0)
            return BadRequest();

        if (payload.Events.Count > 100)
            return BadRequest("Too many events in a single batch");

        var userAgent = Request.Headers.UserAgent.ToString();
        await trackingService.StoreEventsAsync(payload, userAgent);
        return NoContent();
    }
}
