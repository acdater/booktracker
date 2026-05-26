using BookTracker.Api.DTOs.Auth;

namespace BookTracker.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterDto dto);
    Task<AuthResponse> LoginAsync(LoginDto dto);
}
