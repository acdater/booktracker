using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IBookRepository
{
    Task<Book?> GetByISBNAsync(string isbn);
    Task<Book?> GetByIdAsync(int id);
    Task<Book> CreateAsync(Book book);
}
