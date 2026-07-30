using AutoMapper;
using GeoLibrary.Server.Abstractions;
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
    private readonly IStorageService _storageService;
    private readonly ILogger<BookController> _logger;

    public BookController(IHttpContextAccessor contextAccessor, GeoLibraryDbContext db, IMapper mapper, IStorageService storageService, ILogger<BookController> logger)
    {
        _contextAccessor = contextAccessor;
        _db = db;
        _mapper = mapper;
        _storageService = storageService;
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

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> DeleteBook(Guid bookId)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var book = await _db.Books.Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return NotFound();
        }

        if (book.Library.UserId != userId)
        {
            return Forbid();
        }

        // Prenotazioni e viste giornaliere sono in cascata sul database.
        _db.Books.Remove(book);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{bookId}/cover")]
    public async Task<IActionResult> UploadCover(Guid bookId, IFormFile file)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var book = await _db.Books.Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return NotFound();
        }
        if (book.Library.UserId != userId)
        {
            return Forbid();
        }
        using var stream = file.OpenReadStream();
        var key = await _storageService.UploadImage(stream, file.FileName, file.ContentType);
        book.CoverImageKey = key;
        await _db.SaveChangesAsync();
        return Ok(new
        {
            CoverImageUrl = await _storageService.GetUrl(key),
            CoverThumbnailUrl = await _storageService.GetThumbnailUrl(key)
        });
    }

    [HttpPut("{bookId}")]
    public async Task<IActionResult> UpdateBook(Guid bookId, AddBookDto bookDto)
    {
        ArgumentNullException.ThrowIfNull(bookDto);

        // Si riusa AddBookDto, quindi il client deve mandare anche LibraryId anche se qui non serve.
        var validator = new AddBookDtoValidator();
        var validationResult = await validator.ValidateAsync(bookDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var book = await _db.Books.Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return NotFound();
        }

        if (book.Library.UserId != userId)
        {
            return Forbid();
        }

        // Niente mapper: LibraryId sposterebbe il libro in una libreria di qualcun altro.
        book.Title = bookDto.Title;
        book.Description = bookDto.Description;
        book.Author = bookDto.Author;
        book.ISBN = bookDto.ISBN;
        book.TotalCopies = bookDto.TotalCopies;
        book.IsHidden = bookDto.IsHidden;

        await _db.SaveChangesAsync();

        var dto = _mapper.Map<BookDto>(book);
        if (!string.IsNullOrEmpty(book.CoverImageKey))
        {
            dto.CoverImageUrl = await _storageService.GetUrl(book.CoverImageKey);
            dto.CoverThumbnailUrl = await _storageService.GetThumbnailUrl(book.CoverImageKey);
        }

        return Ok(dto);
    }

}
