using BookTracker.Api.DTOs.Shelf;

namespace BookTracker.Api.Services.Interfaces;

public interface IShelfService
{
    Task<UserBookResponse> AddToShelfAsync(int userId, int bookId);
    Task<List<UserBookResponse>> GetShelfAsync(int userId);
    Task<UserBookResponse> UpdateStatusAsync(int userId, int userBookId, string status);
    Task<UserBookResponse> UpdatePagesAsync(int userId, int userBookId, int pages);
    Task<List<JournalEntryResponse>> GetJournalAsync(int userId, int userBookId);
    Task<UserBookResponse> RereadAsync(int userId, int userBookId);
}
