using BookTracker.Api.DTOs.Books;
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

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
    {
        var result = await bookService.CreateBookAsync(dto);
        return result.IsNew ? StatusCode(201, result.Response) : Ok(result.Response);
    }
}
