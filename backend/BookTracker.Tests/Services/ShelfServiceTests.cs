using BookTracker.Api.Exceptions;
using BookTracker.Api.Models;
using BookTracker.Api.Models.Enums;
using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services;
using Moq;

namespace BookTracker.Tests.Services;

public class ShelfServiceTests
{
    private readonly Mock<IUserBookRepository> _userBookRepoMock = new();
    private readonly Mock<IBookRepository> _bookRepoMock = new();

    private ShelfService CreateSut() => new(_userBookRepoMock.Object, _bookRepoMock.Object);

    private static Book MakeBook(int id = 1) => new()
    {
        Id = id,
        ISBN = $"978000000000{id}",
        Title = $"Book {id}",
        Author = "Author",
        TotalPages = 300,
        Genre = "Fiction"
    };

    private static UserBook MakeUserBook(int id, int userId, Book book, int readingNumber = 1, DateTime? lastActivity = null) => new()
    {
        Id = id,
        UserId = userId,
        BookId = book.Id,
        Book = book,
        Status = ReadingStatus.Resting,
        CurrentPages = 0,
        ReadingNumber = readingNumber,
        LastActivityAt = lastActivity ?? DateTime.UtcNow
    };

    // ── AddToShelfAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddToShelfAsync_BookExists_CreatesUserBookWithCorrectInitialValues()
    {
        var book = MakeBook(42);
        var userId = 7;
        _bookRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(book);

        UserBook? captured = null;
        _userBookRepoMock.Setup(r => r.CreateAsync(It.IsAny<UserBook>()))
            .Callback<UserBook>(ub => captured = ub)
            .ReturnsAsync((UserBook ub) => ub);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [42] = 5 });

        var sut = CreateSut();
        var result = await sut.AddToShelfAsync(userId, 42);

        Assert.NotNull(captured);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(42, captured.BookId);
        Assert.Equal(ReadingStatus.Resting, captured.Status);
        Assert.Equal(0, captured.CurrentPages);
        Assert.Equal(1, captured.ReadingNumber);
        Assert.True((DateTime.UtcNow - captured.LastActivityAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task AddToShelfAsync_BookExists_ReturnsUserBookResponseWithReaderCount()
    {
        var book = MakeBook(42);
        _bookRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync(book);
        _userBookRepoMock.Setup(r => r.CreateAsync(It.IsAny<UserBook>()))
            .ReturnsAsync((UserBook ub) => ub);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [42] = 3 });

        var sut = CreateSut();
        var result = await sut.AddToShelfAsync(1, 42);

        Assert.Equal("Resting", result.Status);
        Assert.Equal(3, result.ReaderCount);
        Assert.Equal(42, result.Book.Id);
        Assert.Equal(book.Title, result.Book.Title);
        Assert.Null(result.StartedAt);
        Assert.Null(result.FinishedAt);
    }

    [Fact]
    public async Task AddToShelfAsync_BookNotFound_Throws404ApiException()
    {
        _bookRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Book?)null);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.AddToShelfAsync(1, 99));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("NOT_FOUND", ex.ErrorCode);
    }

    // ── GetShelfAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShelfAsync_EmptyShelf_ReturnsEmptyList()
    {
        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([]);

        var sut = CreateSut();
        var result = await sut.GetShelfAsync(1);

        Assert.Empty(result);
        _userBookRepoMock.Verify(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [Fact]
    public async Task GetShelfAsync_MultipleReadings_ReturnsOnlyHighestReadingNumberPerBook()
    {
        var book = MakeBook(10);
        var old = MakeUserBook(1, 1, book, readingNumber: 1, lastActivity: DateTime.UtcNow.AddDays(-5));
        var latest = MakeUserBook(2, 1, book, readingNumber: 2, lastActivity: DateTime.UtcNow.AddDays(-1));

        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([old, latest]);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

        var sut = CreateSut();
        var result = await sut.GetShelfAsync(1);

        Assert.Single(result);
        Assert.Equal(2, result[0].ReadingNumber);
    }

    [Fact]
    public async Task GetShelfAsync_MultipleBooks_OrderedByLastActivityAtDesc()
    {
        var book1 = MakeBook(1);
        var book2 = MakeBook(2);
        var now = DateTime.UtcNow;
        var ub1 = MakeUserBook(1, 1, book1, lastActivity: now.AddDays(-3));
        var ub2 = MakeUserBook(2, 1, book2, lastActivity: now.AddDays(-1));

        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([ub1, ub2]);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 1, [2] = 2 });

        var sut = CreateSut();
        var result = await sut.GetShelfAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal(book2.Id, result[0].Book.Id);  // most recent first
        Assert.Equal(book1.Id, result[1].Book.Id);
    }

    [Fact]
    public async Task GetShelfAsync_ReaderCountMissingForBook_DefaultsToZero()
    {
        var book = MakeBook(99);
        var ub = MakeUserBook(1, 1, book);

        _userBookRepoMock.Setup(r => r.GetShelfAsync(1)).ReturnsAsync([ub]);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int>()); // empty — no counts

        var sut = CreateSut();
        var result = await sut.GetShelfAsync(1);

        Assert.Single(result);
        Assert.Equal(0, result[0].ReaderCount);
    }
}
