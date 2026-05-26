using BookTracker.Api.DTOs.Shelf;

namespace BookTracker.Api.Services.Interfaces;

public interface IShelfService
{
    Task<UserBookResponse> AddToShelfAsync(int userId, int bookId);
    Task<List<UserBookResponse>> GetShelfAsync(int userId);
    Task<UserBookResponse> UpdateStatusAsync(int userId, int userBookId, string status);
}
