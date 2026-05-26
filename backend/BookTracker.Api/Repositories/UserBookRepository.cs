using BookTracker.Api.Data;
using BookTracker.Api.Models;
using BookTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Api.Repositories;

public class UserBookRepository(AppDbContext db) : IUserBookRepository
{
    private readonly AppDbContext _db = db;

    public async Task<List<UserBook>> GetShelfAsync(int userId) =>
        await _db.UserBooks
            .Include(ub => ub.Book)
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.LastActivityAt)
            .ToListAsync();

    public async Task<UserBook?> GetByIdAsync(int id) =>
        await _db.UserBooks
            .Include(ub => ub.Book)
            .FirstOrDefaultAsync(ub => ub.Id == id);

    public async Task<UserBook> CreateAsync(UserBook ub)
    {
        _db.UserBooks.Add(ub);
        await _db.SaveChangesAsync();
        return ub;
    }

    public async Task<UserBook> UpdateAsync(UserBook ub)
    {
        _db.UserBooks.Update(ub);
        await _db.SaveChangesAsync();
        return ub;
    }

    public async Task<int> GetMaxReadingNumberAsync(int userId, int bookId) =>
        await _db.UserBooks
            .Where(ub => ub.UserId == userId && ub.BookId == bookId)
            .MaxAsync(ub => (int?)ub.ReadingNumber) ?? 0;
}
