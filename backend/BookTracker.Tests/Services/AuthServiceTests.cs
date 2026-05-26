using BookTracker.Api.DTOs.Auth;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BookTracker.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly IConfiguration _config;

    public AuthServiceTests()
    {
        var dict = new Dictionary<string, string?>
        {
            ["JWT__Secret"] = "test-secret-key-must-be-at-least-32-bytes-long!",
            ["JWT:ExpiryHours"] = "24"
        };
        _config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private AuthService CreateSut() => new(_repoMock.Object, _config);

    private static RegisterDto ValidDto() => new()
    {
        Email = "alice@example.com",
        Password = "Password1!",
        FirstName = "Alice",
        LastName = "Smith",
        DateOfBirth = new DateTime(1990, 1, 1)
    };

    [Fact]
    public async Task RegisterAsync_HappyPath_ReturnsAuthResponse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByEmailAsync("alice@example.com")).ReturnsAsync((User?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = 1; return u; });

        // Act
        var result = await CreateSut().RegisterAsync(ValidDto());

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.FirstName);
        Assert.False(string.IsNullOrEmpty(result.Token));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsApiException409()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync(new User { Email = "alice@example.com" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateSut().RegisterAsync(ValidDto()));
        Assert.Equal(409, ex.StatusCode);
        Assert.Equal("EMAIL_EXISTS", ex.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashedPassword()
    {
        // Arrange
        User? captured = null;
        _repoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => captured = u)
            .ReturnsAsync((User u) => { u.Id = 2; return u; });

        // Act
        await CreateSut().RegisterAsync(ValidDto());

        // Assert
        Assert.NotNull(captured);
        Assert.NotEqual("Password1!", captured!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password1!", captured.PasswordHash));
    }
}
