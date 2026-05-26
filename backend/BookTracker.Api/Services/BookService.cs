using System.Net.Http.Json;
using System.Text.Json;
using BookTracker.Api.DTOs.Books;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Api.Services;

public class BookService(IBookRepository bookRepository, IHttpClientFactory httpClientFactory) : IBookService
{
    private static readonly HashSet<string> AllowedGenres = new()
    {
        "Fiction", "Non-Fiction", "Mystery", "Science Fiction", "Fantasy",
        "Romance", "Biography & Memoir", "History", "Self-Help", "Other"
    };

    public async Task<BookResponse?> LookupISBNAsync(string isbn)
    {
        isbn = isbn.Trim().ToUpperInvariant();

        // 1. Catalog hit — return immediately, no Open Library call
        var existing = await bookRepository.GetByISBNAsync(isbn);
        if (existing is not null)
            return MapToResponse(existing);

        // 2. Open Library fallback — catch ALL failures
        try
        {
            var client = httpClientFactory.CreateClient("OpenLibrary");
            var url = $"/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data";
            var response = await client.GetFromJsonAsync<Dictionary<string, JsonElement>>(url);

            var key = $"ISBN:{isbn}";
            if (response is null || !response.TryGetValue(key, out var bookData))
                return null;

            return MapOpenLibraryResponse(isbn, bookData);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(BookResponse Response, bool IsNew)> CreateBookAsync(CreateBookDto dto)
    {
        if (!AllowedGenres.Contains(dto.Genre))
            throw new ApiException(400,
                $"Genre must be one of: {string.Join(", ", AllowedGenres)}.",
                "VALIDATION_ERROR");

        var book = new Book
        {
            ISBN = dto.ISBN,
            Title = dto.Title,
            Author = dto.Author,
            TotalPages = dto.TotalPages,
            Genre = dto.Genre,
            CoverImageUrl = dto.CoverImageUrl
        };

        try
        {
            book = await bookRepository.CreateAsync(book);
            return (MapToResponse(book), true);
        }
        catch (DbUpdateException)
        {
            var existing = await bookRepository.GetByISBNAsync(dto.ISBN);
            return (MapToResponse(existing!), false);
        }
    }

    private static BookResponse MapToResponse(Book book) => new()
    {
        Id = book.Id,
        ISBN = book.ISBN,
        Title = book.Title,
        Author = book.Author,
        TotalPages = book.TotalPages,
        Genre = book.Genre,
        CoverImageUrl = book.CoverImageUrl
    };

    private static BookResponse MapOpenLibraryResponse(string isbn, JsonElement data)
    {
        var title = data.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";

        var author = "";
        if (data.TryGetProperty("authors", out var authors) && authors.GetArrayLength() > 0)
            author = authors[0].TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";

        var totalPages = 0;
        if (data.TryGetProperty("number_of_pages", out var pages))
            totalPages = pages.GetInt32();

        string? coverImageUrl = null;
        if (data.TryGetProperty("cover", out var cover) &&
            cover.TryGetProperty("medium", out var medium))
            coverImageUrl = medium.GetString();

        return new BookResponse
        {
            Id = 0,
            ISBN = isbn,
            Title = title,
            Author = author,
            TotalPages = totalPages,
            Genre = null,
            CoverImageUrl = coverImageUrl
        };
    }
}
