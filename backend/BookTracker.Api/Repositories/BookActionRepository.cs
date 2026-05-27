using BookTracker.Api.Data;
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookTracker.Api.Repositories;

public class BookActionRepository(AppDbContext db) : IBookActionRepository
{
    private readonly AppDbContext _db = db;

    public async Task AddAsync(BookAction action)
    {
        _db.BookActions.Add(action);
        await _db.SaveChangesAsync();
    }

    public async Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId) =>
        await _db.BookActions
            .Where(a => a.UserId == userId && a.UserBookId == userBookId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

    public async Task<List<BookAction>> GetJournalAsync(int userId, int bookId) =>
        await _db.BookActions
            .Include(a => a.UserBook)
            .Where(a => a.UserId == userId && a.UserBook.BookId == bookId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

    public async Task<List<BookAction>> GetPageUpdatesInMonthAsync(int userId, int year, int month) =>
        await _db.BookActions
            .Where(a => a.UserId == userId
                && a.ActionType == ActionType.PageUpdate
                && a.Timestamp.Year == year
                && a.Timestamp.Month == month)
            .ToListAsync();

    public async Task<List<BookAction>> GetStatusChangesCompletedSinceAsync(int userId, DateTime since) =>
        await _db.BookActions
            .Where(a => a.UserId == userId
                && a.ActionType == ActionType.StatusChange
                && a.NewValue == "Finished"
                && a.Timestamp >= since)
            .ToListAsync();

    public async Task<List<BookAction>> GetPageUpdatesSinceAsync(int userId, DateTime since) =>
        await _db.BookActions
            .Where(a => a.UserId == userId
                && a.ActionType == ActionType.PageUpdate
                && a.Timestamp >= since)
            .ToListAsync();
}
