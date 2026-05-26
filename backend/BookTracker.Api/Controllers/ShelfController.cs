using System.Security.Claims;
using BookTracker.Api.DTOs.Shelf;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("api/shelf")]
[Authorize]
public class ShelfController(IShelfService shelfService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddToShelf([FromBody] AddToShelfDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.AddToShelfAsync(userId, dto.BookId);
        return StatusCode(201, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetShelf()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.GetShelfAsync(userId);
        return Ok(result);
    }

    [HttpPatch("{userBookId}/status")]
    public async Task<IActionResult> UpdateStatus(int userBookId, [FromBody] UpdateStatusDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await shelfService.UpdateStatusAsync(userId, userBookId, dto.Status);
        return Ok(result);
    }
}
