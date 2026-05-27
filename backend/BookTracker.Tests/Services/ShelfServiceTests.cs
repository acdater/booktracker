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
    private readonly Mock<IBookActionRepository> _bookActionRepoMock = new();

    private ShelfService CreateSut() => new(
        _userBookRepoMock.Object,
        _bookRepoMock.Object,
        _bookActionRepoMock.Object);

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

    // ── UpdateStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ValidRestingToStarted_UpdatesStatusAndCreatesAction()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 7, book);

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        UserBook? capturedUb = null;
        BookAction? capturedAction = null;
        _userBookRepoMock
            .Setup(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()))
            .Callback<UserBook, BookAction>((u, a) => { capturedUb = u; capturedAction = a; })
            .ReturnsAsync((UserBook u, BookAction _) => u);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

        var sut = CreateSut();
        var result = await sut.UpdateStatusAsync(7, 1, "Started");

        Assert.NotNull(capturedUb);
        Assert.Equal(ReadingStatus.Started, capturedUb.Status);
        Assert.NotNull(capturedUb.StartedAt);
        Assert.True((DateTime.UtcNow - capturedUb.LastActivityAt).TotalSeconds < 5);

        Assert.NotNull(capturedAction);
        Assert.Equal(ActionType.StatusChange, capturedAction.ActionType);
        Assert.Equal("Resting", capturedAction.OldValue);
        Assert.Equal("Started", capturedAction.NewValue);
        Assert.Equal(7, capturedAction.UserId);
        Assert.Equal(1, capturedAction.UserBookId);

        Assert.Equal("Started", result.Status);
        _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_Throws400WithInvalidTransitionCode()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Abandoned;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdateStatusAsync(7, 1, "Started"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_TRANSITION", ex.ErrorCode);
        _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_OwnershipMismatch_Throws403()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 99, book); // owned by user 99

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdateStatusAsync(1, 1, "Started"));

        Assert.Equal(403, ex.StatusCode);
        Assert.Equal("FORBIDDEN", ex.ErrorCode);
    }

    // ── UpdatePagesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePagesAsync_NormalUpdate_StoresPageUpdateAction()
    {
        var book = MakeBook(10); // TotalPages = 300
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Started;
        ub.CurrentPages = 50;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        UserBook? capturedUb = null;
        BookAction? capturedAction = null;
        _userBookRepoMock
            .Setup(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()))
            .Callback<UserBook, BookAction>((u, a) => { capturedUb = u; capturedAction = a; })
            .ReturnsAsync((UserBook u, BookAction _) => u);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

        var sut = CreateSut();
        var result = await sut.UpdatePagesAsync(7, 1, 100);

        Assert.Equal(100, capturedUb!.CurrentPages);
        Assert.Equal(ReadingStatus.Started, capturedUb.Status);
        Assert.Null(capturedUb.FinishedAt);
        Assert.Equal(ActionType.PageUpdate, capturedAction!.ActionType);
        Assert.Equal("50", capturedAction.OldValue);
        Assert.Equal("100", capturedAction.NewValue);
        Assert.Equal("Started", result.Status);
        _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Once);
        _userBookRepoMock.Verify(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePagesAsync_AutoFinish_StoresTwoBookActionsAndFinishedStatus()
    {
        var book = MakeBook(10); // TotalPages = 300
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Started;
        ub.CurrentPages = 280;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        IReadOnlyList<BookAction>? capturedActions = null;
        UserBook? capturedUb = null;
        _userBookRepoMock
            .Setup(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()))
            .Callback<UserBook, IReadOnlyList<BookAction>>((u, a) => { capturedUb = u; capturedActions = a; })
            .ReturnsAsync((UserBook u, IReadOnlyList<BookAction> _) => u);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [10] = 1 });

        var sut = CreateSut();
        var result = await sut.UpdatePagesAsync(7, 1, 300); // 300 == TotalPages

        Assert.Equal(ReadingStatus.Finished, capturedUb!.Status);
        Assert.NotNull(capturedUb.FinishedAt);
        Assert.Equal(300, capturedUb.CurrentPages);

        Assert.NotNull(capturedActions);
        Assert.Equal(2, capturedActions.Count);
        Assert.Contains(capturedActions, a => a.ActionType == ActionType.PageUpdate && a.NewValue == "300");
        Assert.Contains(capturedActions, a => a.ActionType == ActionType.StatusChange && a.OldValue == "Started" && a.NewValue == "Finished");

        Assert.Equal("Finished", result.Status);
        _userBookRepoMock.Verify(r => r.UpdateWithActionsAsync(It.IsAny<UserBook>(), It.IsAny<IReadOnlyList<BookAction>>()), Times.Once);
        _userBookRepoMock.Verify(r => r.UpdateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePagesAsync_PagesExceedTotal_Throws400InvalidPage()
    {
        var book = MakeBook(10); // TotalPages = 300
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Started;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdatePagesAsync(7, 1, 999));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_PAGE", ex.ErrorCode);
    }

    [Fact]
    public async Task UpdatePagesAsync_StatusNotStarted_Throws400InvalidState()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Resting; // not Started

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.UpdatePagesAsync(7, 1, 100));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_STATE", ex.ErrorCode);
    }

    // ── GetJournalAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetJournalAsync_ValidRequest_ReturnsJournalEntriesAcrossReadingNumbers()
    {
        var book = MakeBook(10);
        var ub1 = MakeUserBook(1, 7, book, readingNumber: 1);
        var ub2 = MakeUserBook(2, 7, book, readingNumber: 2);

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub1);

        var actions = new List<BookAction>
        {
            new() { Id = 1, UserId = 7, UserBookId = 2, UserBook = ub2,
                    ActionType = ActionType.StatusChange, OldValue = "Resting", NewValue = "Started",
                    Timestamp = DateTime.UtcNow },
            new() { Id = 2, UserId = 7, UserBookId = 1, UserBook = ub1,
                    ActionType = ActionType.PageUpdate, OldValue = "0", NewValue = "100",
                    Timestamp = DateTime.UtcNow.AddDays(-1) }
        };

        _bookActionRepoMock.Setup(r => r.GetJournalAsync(7, book.Id)).ReturnsAsync(actions);

        var sut = CreateSut();
        var result = await sut.GetJournalAsync(7, 1);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].ReadingNumber);
        Assert.Equal("Status Change", result[0].ActionType);
        Assert.Equal(1, result[1].ReadingNumber);
        Assert.Equal("Page Update", result[1].ActionType);
    }

    [Fact]
    public async Task GetJournalAsync_WrongOwner_Throws403()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 99, book); // owned by userId=99

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.GetJournalAsync(7, 1));

        Assert.Equal(403, ex.StatusCode);
    }

    // ── RereadAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RereadAsync_FinishedBook_CreatesNewUserBookWithCorrectValues()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Finished;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);
        _userBookRepoMock.Setup(r => r.GetMaxReadingNumberAsync(7, book.Id)).ReturnsAsync(1);

        UserBook? capturedUb = null;
        BookAction? capturedAction = null;
        _userBookRepoMock.Setup(r => r.CreateWithActionAsync(It.IsAny<UserBook>(), It.IsAny<BookAction>()))
            .Callback<UserBook, BookAction>((u, a) => { capturedUb = u; capturedAction = a; })
            .ReturnsAsync((UserBook u, BookAction _) => u);
        _userBookRepoMock.Setup(r => r.GetReaderCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [book.Id] = 1 });

        var sut = CreateSut();
        var result = await sut.RereadAsync(7, 1);

        Assert.NotNull(capturedUb);
        Assert.Equal(ReadingStatus.Started, capturedUb.Status);
        Assert.Equal(0, capturedUb.CurrentPages);
        Assert.Equal(2, capturedUb.ReadingNumber); // MAX(1) + 1
        Assert.NotNull(capturedUb.StartedAt);
        Assert.Null(capturedUb.FinishedAt);
        Assert.Equal("Started", result.Status);

        Assert.NotNull(capturedAction);
        Assert.Equal(ActionType.StatusChange, capturedAction.ActionType);
        Assert.Equal("Resting", capturedAction.OldValue);
        Assert.Equal("Started", capturedAction.NewValue);
    }

    [Fact]
    public async Task RereadAsync_StartedBook_Throws400InvalidState()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 7, book);
        ub.Status = ReadingStatus.Started;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.RereadAsync(7, 1));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("INVALID_STATE", ex.ErrorCode);
    }

    [Fact]
    public async Task RereadAsync_WrongOwner_Throws403()
    {
        var book = MakeBook(10);
        var ub = MakeUserBook(1, 99, book); // owned by userId=99
        ub.Status = ReadingStatus.Finished;

        _userBookRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ub);

        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ApiException>(() => sut.RereadAsync(7, 1));

        Assert.Equal(403, ex.StatusCode);
    }
}
