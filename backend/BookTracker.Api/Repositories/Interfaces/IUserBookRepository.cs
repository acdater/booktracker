using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IUserBookRepository
{
    Task<List<UserBook>> GetShelfAsync(int userId);
    Task<UserBook?> GetByIdAsync(int id);
    Task<UserBook> CreateAsync(UserBook ub);
    Task<UserBook> CreateWithActionAsync(UserBook ub, BookAction action);
    Task<UserBook> UpdateAsync(UserBook ub);
    Task<UserBook> UpdateWithActionAsync(UserBook ub, BookAction action);
    Task<UserBook> UpdateWithActionsAsync(UserBook ub, IReadOnlyList<BookAction> actions);
    Task<int> GetMaxReadingNumberAsync(int userId, int bookId);
    Task<Dictionary<int, int>> GetReaderCountsAsync(IEnumerable<int> bookIds);
}
