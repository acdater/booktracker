using BookTracker.Api.DTOs.Books;

namespace BookTracker.Api.Services.Interfaces;

public interface IBookService
{
    Task<BookResponse?> LookupISBNAsync(string isbn);
    Task<(BookResponse Response, bool IsNew)> CreateBookAsync(CreateBookDto dto);
}
