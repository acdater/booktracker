using BookTracker.Api.DTOs.Books;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BookTracker.Tests.Services;

public class BookCatalogServiceTests
{
    private readonly Mock<IBookRepository> _repoMock = new();
    private readonly Mock<IHttpClientFactory> _factoryMock = new();

    private BookService CreateSut() => new(_repoMock.Object, _factoryMock.Object);

    private static CreateBookDto ValidDto(string isbn = "9780140328721") => new()
    {
        ISBN = isbn,
        Title = "Fantastic Mr. Fox",
        Author = "Roald Dahl",
        TotalPages = 96,
        Genre = "Fiction",
        CoverImageUrl = "https://covers.openlibrary.org/b/id/123-M.jpg"
    };

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookAsync_ValidInput_ReturnsResponseWithIsNewTrue()
    {
        var dto = ValidDto();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Book>()))
            .ReturnsAsync((Book b) => { b.Id = 42; return b; });

        var (response, isNew) = await CreateSut().CreateBookAsync(dto);

        Assert.True(isNew);
        Assert.Equal(42, response.Id);
        Assert.Equal(dto.ISBN, response.ISBN);
        Assert.Equal(dto.Title, response.Title);
        Assert.Equal(dto.Author, response.Author);
        Assert.Equal(dto.TotalPages, response.TotalPages);
        Assert.Equal(dto.Genre, response.Genre);
        Assert.Equal(dto.CoverImageUrl, response.CoverImageUrl);
    }

    // ── Duplicate ISBN (deduplication) ───────────────────────────────────────

    [Fact]
    public async Task CreateBookAsync_DuplicateISBN_ReturnsExistingResponseWithIsNewFalse()
    {
        var dto = ValidDto();
        var existingBook = new Book
        {
            Id = 7,
            ISBN = dto.ISBN,
            Title = "Existing Title",
            Author = "Existing Author",
            TotalPages = 200,
            Genre = "Mystery"
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Book>()))
            .ThrowsAsync(new DbUpdateException("Duplicate key", new Exception()));
        _repoMock.Setup(r => r.GetByISBNAsync(dto.ISBN))
            .ReturnsAsync(existingBook);

        var (response, isNew) = await CreateSut().CreateBookAsync(dto);

        Assert.False(isNew);
        Assert.Equal(7, response.Id);
        Assert.Equal("Existing Title", response.Title);
        _repoMock.Verify(r => r.GetByISBNAsync(dto.ISBN), Times.Once);
    }

    // ── Invalid genre ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBookAsync_InvalidGenre_ThrowsApiException400()
    {
        var dto = ValidDto();
        dto.Genre = "InvalidGenre";

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateSut().CreateBookAsync(dto));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("VALIDATION_ERROR", ex.ErrorCode);
        Assert.Contains("Genre must be one of:", ex.Message);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Book>()), Times.Never);
    }

    // ── All allowed genres pass validation ───────────────────────────────────

    [Theory]
    [InlineData("Fiction")]
    [InlineData("Non-Fiction")]
    [InlineData("Mystery")]
    [InlineData("Science Fiction")]
    [InlineData("Fantasy")]
    [InlineData("Romance")]
    [InlineData("Biography & Memoir")]
    [InlineData("History")]
    [InlineData("Self-Help")]
    [InlineData("Other")]
    public async Task CreateBookAsync_AllowedGenre_DoesNotThrow(string genre)
    {
        var dto = ValidDto();
        dto.Genre = genre;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Book>()))
            .ReturnsAsync((Book b) => { b.Id = 1; return b; });

        var (_, isNew) = await CreateSut().CreateBookAsync(dto);

        Assert.True(isNew);
    }
}
