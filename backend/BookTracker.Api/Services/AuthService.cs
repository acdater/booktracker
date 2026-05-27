using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookTracker.Api.DTOs.Auth;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace BookTracker.Api.Services;

public class AuthService(IUserRepository userRepository, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
    {
        var existing = await userRepository.GetByEmailAsync(dto.Email);
        if (existing is not null)
            throw new ApiException(409, "Email is already registered.", "EMAIL_EXISTS");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth!.Value, DateTimeKind.Utc)
        };

        user = await userRepository.CreateAsync(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = GenerateJwt(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new ApiException(401, "Invalid credentials.", "INVALID_CREDENTIALS");

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = GenerateJwt(user)
        };
    }

    private string GenerateJwt(User user)
    {
        var secret = configuration["JWT__Secret"]!;
        var expiryHours = configuration.GetValue<int>("JWT:ExpiryHours", 24);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
