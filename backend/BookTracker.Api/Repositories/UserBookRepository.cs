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

    public async Task<UserBook> CreateWithActionAsync(UserBook ub, BookAction action)
    {
        _db.UserBooks.Add(ub);
        _db.BookActions.Add(action);
        await _db.SaveChangesAsync();
        return ub;
    }

    public async Task<UserBook> UpdateWithActionAsync(UserBook ub, BookAction action)
    {
        _db.UserBooks.Update(ub);
        _db.BookActions.Add(action);
        await _db.SaveChangesAsync();
        return ub;
    }

    public async Task<UserBook> UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions)
    {
        _db.UserBooks.Update(ub);
        _db.BookActions.AddRange(actions);
        await _db.SaveChangesAsync();
        return ub;
    }

    public async Task<int> GetMaxReadingNumberAsync(int userId, int bookId) =>
        await _db.UserBooks
            .Where(ub => ub.UserId == userId && ub.BookId == bookId)
            .MaxAsync(ub => (int?)ub.ReadingNumber) ?? 0;

    public async Task<Dictionary<int, int>> GetReaderCountsAsync(IEnumerable<int> bookIds)
    {
        var ids = bookIds.ToList();
        return await _db.UserBooks
            .Where(ub => ids.Contains(ub.BookId))
            .GroupBy(ub => ub.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Select(ub => ub.UserId).Distinct().Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);
    }
}
