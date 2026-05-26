using BookTracker.Api.Models;

namespace BookTracker.Api.Repositories.Interfaces;

public interface IBookActionRepository
{
    Task AddAsync(BookAction action);
    Task<List<BookAction>> GetByUserAndBookAsync(int userId, int userBookId);
}
