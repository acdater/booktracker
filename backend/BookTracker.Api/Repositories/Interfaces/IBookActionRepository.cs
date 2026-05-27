using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IBookActionRepository
{
    Task AddAsync(BookAction action);
    Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId);
    Task<List<BookAction>> GetJournalAsync(int userId, int bookId);
    Task<List<BookAction>> GetPageUpdatesInMonthAsync(int userId, int year, int month);
    Task<List<BookAction>> GetStatusChangesCompletedSinceAsync(int userId, DateTime since);
    Task<List<BookAction>> GetPageUpdatesSinceAsync(int userId, DateTime since);
}
