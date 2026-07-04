using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BookController : ControllerBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly GeoLibraryDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<BookController> _logger;

    public BookController(IHttpContextAccessor contextAccessor, GeoLibraryDbContext db, IMapper mapper, ILogger<BookController> logger)
    {
        _contextAccessor = contextAccessor;
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddBook(AddBookDto bookDto)
    {
        ArgumentNullException.ThrowIfNull(bookDto);

        var validator = new AddBookDtoValidator();
        var validationResult = await validator.ValidateAsync(bookDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var library = await _db.Libraries.FirstOrDefaultAsync(x => x.Id == bookDto.LibraryId);
        if (library is null)
        {
            return NotFound();
        }

        if (library.UserId != userId)
        {
            return Forbid();
        }

        var entity = _mapper.Map<AddBookDto, BookEntity>(bookDto);
        entity.Library = library;

        await _db.Books.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Ok(_mapper.Map<BookDto>(entity));
    }
}
