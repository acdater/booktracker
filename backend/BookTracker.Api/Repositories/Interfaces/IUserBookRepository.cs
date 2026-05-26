using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IUserBookRepository
{
    Task<List<UserBook>> GetShelfAsync(int userId);
    Task<UserBook?> GetByIdAsync(int id);
    Task<UserBook> CreateAsync(UserBook ub);
    Task<UserBook> UpdateAsync(UserBook ub);
    Task<int> GetMaxReadingNumberAsync(int userId, int bookId);
}
