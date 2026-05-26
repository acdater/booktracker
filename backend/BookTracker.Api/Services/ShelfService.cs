using BookTracker.Api.DTOs.Books;
using BookTracker.Api.DTOs.Shelf;
using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;

namespace BookTracker.Api.Services;

public class ShelfService(IUserBookRepository userBookRepository, IBookRepository bookRepository) : IShelfService
{
    private readonly IUserBookRepository _userBookRepository = userBookRepository;
    private readonly IBookRepository _bookRepository = bookRepository;

    public async Task<UserBookResponse> AddToShelfAsync(int userId, int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId)
            ?? throw new ApiException(404, "Book not found.", "NOT_FOUND");

        var ub = new UserBook
        {
            UserId = userId,
            BookId = bookId,
            Status = ReadingStatus.Resting,
            CurrentPages = 0,
            ReadingNumber = 1,
            LastActivityAt = DateTime.UtcNow
        };

        ub = await _userBookRepository.CreateAsync(ub);
        ub.Book = book;

        var counts = await _userBookRepository.GetReaderCountsAsync([bookId]);
        return MapToResponse(ub, counts.GetValueOrDefault(bookId, 0));
    }

    public async Task<List<UserBookResponse>> GetShelfAsync(int userId)
    {
        var all = await _userBookRepository.GetShelfAsync(userId);

        var latest = all
            .GroupBy(ub => ub.BookId)
            .Select(g => g.OrderByDescending(ub => ub.ReadingNumber).First())
            .OrderByDescending(ub => ub.LastActivityAt)
            .ToList();

        if (latest.Count == 0) return [];

        var bookIds = latest.Select(ub => ub.BookId).ToList();
        var counts = await _userBookRepository.GetReaderCountsAsync(bookIds);

        return latest.Select(ub => MapToResponse(ub, counts.GetValueOrDefault(ub.BookId, 0))).ToList();
    }

    private static UserBookResponse MapToResponse(UserBook ub, int readerCount) => new()
    {
        Id = ub.Id,
        Book = new BookResponse
        {
            Id = ub.Book.Id,
            ISBN = ub.Book.ISBN,
            Title = ub.Book.Title,
            Author = ub.Book.Author,
            TotalPages = ub.Book.TotalPages,
            Genre = ub.Book.Genre,
            CoverImageUrl = ub.Book.CoverImageUrl
        },
        Status = ub.Status.ToString(),
        CurrentPages = ub.CurrentPages,
        ReadingNumber = ub.ReadingNumber,
        StartedAt = ub.StartedAt,
        FinishedAt = ub.FinishedAt,
        LastActivityAt = ub.LastActivityAt,
        ReaderCount = readerCount
    };
}
