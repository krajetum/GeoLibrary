using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Services;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

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

    private static readonly GeometryFactory _geometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

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

        entity.Categories = await _db.Categories
            .Where(c => bookDto.Categories.Contains(c.Id))
            .ToListAsync();

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
        var key = await _storageService.UploadImageAsync(stream, file.FileName, file.ContentType);
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

        // Le categorie servono caricate: EF confronta la collezione nuova con quella attuale
        // e scrive solo le righe di join cambiate.
        var book = await _db.Books
            .Include(b => b.Library)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == bookId);
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
        book.PublishedAt = bookDto.PublishedAt;
        book.Categories = await _db.Categories
            .Where(c => bookDto.Categories.Contains(c.Id))
            .ToListAsync();

        await _db.SaveChangesAsync();

        var dto = _mapper.Map<BookDto>(book);
        if (!string.IsNullOrEmpty(book.CoverImageKey))
        {
            dto.CoverImageUrl = await _storageService.GetUrl(book.CoverImageKey);
            dto.CoverThumbnailUrl = await _storageService.GetThumbnailUrl(book.CoverImageKey);
        }

        return Ok(dto);
    }

    /// <summary>
    /// Restituisce i libri delle librerie che si trovano entro un raggio (in chilometri) da un punto.
    /// Come per le librerie si usa ST_DWithin con geography, per avere una distanza sferica in metri.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("search/radius")]
    public async Task<IActionResult> SearchByRadius([FromQuery] SearchByRadiusDto dto)
    {
        if (dto.RadiusKilometers <= 0)
            return BadRequest("RadiusKilometers deve essere maggiore di zero.");

        var center = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var query = VisibleBooks(dto.Search)
            .Where(b => EF.Functions.IsWithinDistance(b.Library.Location!, center, dto.RadiusKilometers * 1000, true));

        return Ok(await ToSearchResults(query));
    }

    /// <summary>
    /// Restituisce i libri delle librerie che si trovano dentro un poligono disegnato sulla mappa.
    /// Le coordinate devono essere in ordine; il poligono viene chiuso automaticamente.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("search/polygon")]
    public async Task<IActionResult> SearchByPolygon([FromBody] SearchByPolygonDto dto)
    {
        if (dto.Coordinates.Count < 3)
            return BadRequest("Servono almeno 3 coordinate per definire un poligono.");

        // NTS usa (X = Longitude, Y = Latitude)
        var coords = dto.Coordinates
            .Select(c => new Coordinate(c.Longitude, c.Latitude))
            .ToList();

        // Chiude l'anello ripetendo il primo punto alla fine
        coords.Add(coords[0]);

        var polygon = _geometryFactory.CreatePolygon([.. coords]);

        var query = VisibleBooks(dto.Search)
            .Where(b => polygon.Contains(b.Library.Location!));

        return Ok(await ToSearchResults(query));
    }

    /// <summary>
    /// Libri che compaiono nelle ricerche per area: quelli di librerie non nascoste e con una posizione,
    /// filtrati per titolo, autore o ISBN se è stato scritto qualcosa nella barra di ricerca.
    /// I libri nascosti restano fuori anche per il proprietario: qui si sta guardando la mappa pubblica.
    /// </summary>
    private IQueryable<BookEntity> VisibleBooks(string? search)
    {
        var books = _db.Books
            .AsNoTracking()
            .Where(b => !b.IsHidden && !b.Library.IsHidden && b.Library.Location != null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            books = books.Where(b =>
                EF.Functions.ILike(b.Title, pattern) ||
                EF.Functions.ILike(b.Author, pattern) ||
                EF.Functions.ILike(b.ISBN, pattern));
        }

        return books;
    }

    /// <summary>
    /// Esegue la query e la trasforma nei risultati per la mappa.
    /// Il numero di risultati è limitato: una ricerca su un raggio grande potrebbe restituire tutto il database.
    /// </summary>
    private async Task<List<BookSearchResultDto>> ToSearchResults(IQueryable<BookEntity> query)
    {
        var books = await query
            .OrderBy(b => b.Title)
            .Take(200)
            .Select(b => new
            {
                b.Id,
                b.Title,
                b.Author,
                b.LibraryId,
                LibraryName = b.Library.Name,
                LibraryUserId = b.Library.UserId,
                // NTS usa (X = Longitude, Y = Latitude)
                Latitude = b.Library.Location!.Y,
                Longitude = b.Library.Location!.X,
                b.CoverImageKey
            })
            .ToListAsync();

        _contextAccessor.TryGetUserId(out var userId);
        var withApprovedLoan = await LibrariesWithApprovedLoan(userId);

        var results = new List<BookSearchResultDto>();
        foreach (var b in books)
        {
            // Il filtro spaziale ha già lavorato sulle coordinate vere: qui si arrotonda
            // solo quello che esce, così la ricerca resta esatta ma la risposta no.
            var exactLocation = b.LibraryUserId == userId || withApprovedLoan.Contains(b.LibraryId);

            results.Add(new BookSearchResultDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                LibraryId = b.LibraryId,
                LibraryName = b.LibraryName,
                Latitude = exactLocation ? b.Latitude : Math.Round(b.Latitude, 2),
                Longitude = exactLocation ? b.Longitude : Math.Round(b.Longitude, 2),
                IsApproximateLocation = !exactLocation,
                // Le URL sono firmate e scadono: vanno rigenerate a ogni risposta.
                CoverThumbnailUrl = string.IsNullOrEmpty(b.CoverImageKey)
                    ? null
                    : await _storageService.GetThumbnailUrl(b.CoverImageKey)
            });
        }

        return results;
    }

    /// <summary>
    /// Librerie da cui l'utente ha un prestito approvato in corso: sono quelle di cui
    /// può vedere la posizione esatta, perché deve andarci a ritirare il libro.
    /// </summary>
    private async Task<HashSet<Guid>> LibrariesWithApprovedLoan(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return [];
        }

        var libraryIds = await _db.LoanRequests
            .Where(x => x.UserId == userId && x.Status == LoanRequestStatus.Approved)
            .Join(_db.Books, loan => loan.BookId, book => book.Id, (loan, book) => book.LibraryId)
            .Distinct()
            .ToListAsync();

        return [.. libraryIds];
    }
}
