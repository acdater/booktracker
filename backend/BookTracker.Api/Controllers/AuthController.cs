using BookTracker.Api.DTOs.Auth;
using BookTracker.Api.Services.Interfaces;
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
}
