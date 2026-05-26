using BookTracker.Api.Data;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Api.Repositories;

public class BookRepository(AppDbContext db) : IBookRepository
{
    private readonly AppDbContext _db = db;

    public async Task<Book?> GetByISBNAsync(string isbn) =>
        await _db.Books.FirstOrDefaultAsync(b => b.ISBN == isbn);

    public async Task<Book?> GetByIdAsync(int id) =>
        await _db.Books.FindAsync(id);

    public async Task<Book> CreateAsync(Book book)
    {
        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return book;
    }
}
