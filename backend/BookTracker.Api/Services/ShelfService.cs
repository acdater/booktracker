using BookTracker.Api.Repositories.Interfaces;
using BookTracker.Api.Services.Interfaces;

namespace BookTracker.Api.Services;

public class ShelfService(IUserBookRepository userBookRepository, IBookRepository bookRepository) : IShelfService
{
    private readonly IUserBookRepository _userBookRepository = userBookRepository;
    private readonly IBookRepository _bookRepository = bookRepository;
    // Implementations added in Story 2.4
}
