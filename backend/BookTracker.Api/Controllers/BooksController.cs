using BookTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookTracker.Api.Controllers;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController(IBookService bookService) : ControllerBase
{
    [HttpGet("{isbn}")]
    public async Task<IActionResult> LookupByISBN(string isbn)
    {
        var result = await bookService.LookupISBNAsync(isbn);
        return Ok(result);
    }
}
