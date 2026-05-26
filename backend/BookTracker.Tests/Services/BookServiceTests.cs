using System.Net;
using System.Text;
using System.Text.Json;
using BookTracker.Api.DTOs.Books;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Moq;
using Moq.Protected;

namespace BookTracker.Tests.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _repoMock = new();

    private BookService CreateSut(HttpMessageHandler? handler = null)
    {
        var httpClient = new HttpClient(handler ?? new Mock<HttpMessageHandler>().Object)
        {
            BaseAddress = new Uri("https://openlibrary.org"),
            Timeout = TimeSpan.FromSeconds(3)
        };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("OpenLibrary")).Returns(httpClient);
        return new BookService(_repoMock.Object, factoryMock.Object);
    }

    private static HttpMessageHandler BuildHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        return handlerMock.Object;
    }

    // ── Catalog hit ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_CatalogHit_ReturnsResponseImmediately()
    {
        var book = new Book { Id = 7, ISBN = "9780140328721", Title = "Fantastic Mr. Fox", Author = "Roald Dahl", TotalPages = 96, Genre = "Fiction" };
        _repoMock.Setup(r => r.GetByISBNAsync("9780140328721")).ReturnsAsync(book);

        // No HTTP handler needed — catalog hit should not make any HTTP call
        var factoryMock = new Mock<IHttpClientFactory>();
        var sut = new BookService(_repoMock.Object, factoryMock.Object);

        var result = await sut.LookupISBNAsync("9780140328721");

        Assert.NotNull(result);
        Assert.Equal(7, result!.Id);
        Assert.Equal("Fantastic Mr. Fox", result.Title);
        Assert.Equal("Fiction", result.Genre);
        factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    // ── Open Library match ───────────────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_OpenLibraryMatch_ReturnsMappedResponse()
    {
        _repoMock.Setup(r => r.GetByISBNAsync("9780140328721")).ReturnsAsync((Book?)null);

        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["ISBN:9780140328721"] = new
            {
                title = "Fantastic Mr. Fox",
                authors = new[] { new { name = "Roald Dahl" } },
                number_of_pages = 96,
                cover = new { small = "s.jpg", medium = "m.jpg", large = "l.jpg" }
            }
        });

        var result = await CreateSut(BuildHandler(json)).LookupISBNAsync("9780140328721");

        Assert.NotNull(result);
        Assert.Equal(0, result!.Id);
        Assert.Equal("9780140328721", result.ISBN);
        Assert.Equal("Fantastic Mr. Fox", result.Title);
        Assert.Equal("Roald Dahl", result.Author);
        Assert.Equal(96, result.TotalPages);
        Assert.Null(result.Genre);        // genre is always null from Open Library
        Assert.Equal("m.jpg", result.CoverImageUrl);
    }

    // ── Open Library empty response ──────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_OpenLibraryEmptyResponse_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByISBNAsync(It.IsAny<string>())).ReturnsAsync((Book?)null);

        var result = await CreateSut(BuildHandler("{}")).LookupISBNAsync("0000000000");

        Assert.Null(result);
    }

    // ── Open Library network error ───────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_OpenLibraryHttpRequestException_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByISBNAsync(It.IsAny<string>())).ReturnsAsync((Book?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var result = await CreateSut(handlerMock.Object).LookupISBNAsync("9780140328721");

        Assert.Null(result);
    }

    // ── Open Library timeout ─────────────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_OpenLibraryTimeout_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByISBNAsync(It.IsAny<string>())).ReturnsAsync((Book?)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var result = await CreateSut(handlerMock.Object).LookupISBNAsync("9780140328721");

        Assert.Null(result);
    }

    // ── ISBN normalisation: whitespace ────────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_WhitespaceTrimmed_BeforeLookup()
    {
        var book = new Book { Id = 3, ISBN = "9780140328721", Title = "Fox", Author = "Dahl", TotalPages = 96 };
        _repoMock.Setup(r => r.GetByISBNAsync("9780140328721")).ReturnsAsync(book);

        var factoryMock = new Mock<IHttpClientFactory>();
        var sut = new BookService(_repoMock.Object, factoryMock.Object);

        var result = await sut.LookupISBNAsync("  9780140328721  ");

        Assert.NotNull(result);
        Assert.Equal(3, result!.Id);
        _repoMock.Verify(r => r.GetByISBNAsync("9780140328721"), Times.Once);
    }

    // ── ISBN normalisation: lowercase x ──────────────────────────────────────

    [Fact]
    public async Task LookupISBNAsync_LowercaseX_UppercasedBeforeLookup()
    {
        var book = new Book { Id = 5, ISBN = "080442957X", Title = "Book", Author = "Author", TotalPages = 100 };
        _repoMock.Setup(r => r.GetByISBNAsync("080442957X")).ReturnsAsync(book);

        var factoryMock = new Mock<IHttpClientFactory>();
        var sut = new BookService(_repoMock.Object, factoryMock.Object);

        var result = await sut.LookupISBNAsync("080442957x");

        Assert.NotNull(result);
        _repoMock.Verify(r => r.GetByISBNAsync("080442957X"), Times.Once);
    }
}
