using System.Security.Claims;
using BookTracker.Api.DTOs.Auth;
using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var response = await authService.RegisterAsync(dto);
        return StatusCode(201, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var response = await authService.LoginAsync(dto);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Ok(new { userId });
    }
}
