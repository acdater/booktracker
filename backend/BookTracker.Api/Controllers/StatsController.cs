using System.Security.Claims;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(IStatsService statsService) : ControllerBase
{
    [HttpGet("strip")]
    public async Task<IActionResult> GetStrip()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await statsService.GetStripAsync(userId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await statsService.GetPageAsync(userId);
        return Ok(result);
    }
}
