using AutoMapper;
using AutoMapper.QueryableExtensions;
using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Abstractions.Dtos;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using GeoLibrary.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LibraryController : ControllerBase
{
    private readonly GeoLibraryDbContext _db;
    private readonly ISBNService _isbnService;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILogger<LibraryController> _logger;

    private static readonly GeometryFactory _geometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public LibraryController(GeoLibraryDbContext db, IMapper mapper, ISBNService isbnService, IHttpContextAccessor httpContextAccessor, ILogger<LibraryController> logger)
    {
        _db = db;
        _mapper = mapper;
        _isbnService = isbnService;
        _contextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddLibrary(AddLibraryDto libraryDto)
    {
        ArgumentNullException.ThrowIfNull(libraryDto);

        var validator = new AddLibraryDtoValidator();
        var validationResult = await validator.ValidateAsync(libraryDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if(!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var entity = _mapper.Map<AddLibraryDto, LibraryEntity>(libraryDto);

        // NTS usa (X = Longitude, Y = Latitude)
        entity.Location = _geometryFactory.CreatePoint(
            new Coordinate(libraryDto.Longitude, libraryDto.Latitude));

        entity.UserId = userId; 
        await _db.Libraries.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Ok(libraryDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetLibraries()
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        // Il COUNT viene eseguito lato database (subquery sulla tabella Books), senza caricare i libri.
        var libraries = await _db.Libraries
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .Select(x => new { Entity = x, BookCount = x.Books.LongCount() })
            .ToListAsync();

        var result = libraries.Select(x =>
        {
            var dto = _mapper.Map<LibraryDto>(x.Entity);
            dto.BookCount = x.BookCount;
            return dto;
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLibrary([FromRoute] Guid id)
    {
        // Dettaglio pubblico: non richiediamo l'utente. Se assente, IsAdmin resta false.
        _contextAccessor.TryGetUserId(out var userId);

        // Il COUNT dei libri viene eseguito lato database.
        var result = await _db.Libraries
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { Entity = x, BookCount = x.Books.LongCount() })
            .FirstOrDefaultAsync();

        if (result is null)
        {
            return NotFound();
        }

        var dto = _mapper.Map<LibraryDto>(result.Entity);
        dto.BookCount = result.BookCount;
        dto.IsAdmin = result.Entity.UserId == userId;

        return Ok(dto);
    }

    [HttpGet("{libraryId}/books")]
    public async Task<IActionResult> GetLibraryBooks([FromRoute] Guid libraryId, [FromQuery] GetBooksQueryDto query)
    {
        // Elenco pubblico: come per GetLibrary, non richiediamo l'utente.
        var page = query.Page < 1 ? 1 : query.Page;

        var booksQuery = _db.Books
            .AsNoTracking()
            .Where(x => x.LibraryId == libraryId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            booksQuery = booksQuery.Where(x =>
                EF.Functions.ILike(x.Title, pattern) ||
                EF.Functions.ILike(x.Author, pattern) ||
                EF.Functions.ILike(x.ISBN, pattern));
        }

        booksQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "author" => query.SortDesc ? booksQuery.OrderByDescending(x => x.Author) : booksQuery.OrderBy(x => x.Author),
            "isbn" => query.SortDesc ? booksQuery.OrderByDescending(x => x.ISBN) : booksQuery.OrderBy(x => x.ISBN),
            _ => query.SortDesc ? booksQuery.OrderByDescending(x => x.Title) : booksQuery.OrderBy(x => x.Title),
        };

        var totalCount = await booksQuery.CountAsync();

        // itemsPerPage <= 0 (es. "Tutti" di Vuetify, valore -1) restituisce l'intero risultato senza paginare.
        if (query.ItemsPerPage > 0)
        {
            booksQuery = booksQuery.Skip((page - 1) * query.ItemsPerPage).Take(query.ItemsPerPage);
        }

        var items = await booksQuery
            .ProjectTo<BookDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(new PagedResultDto<BookDto> { Items = items, TotalCount = totalCount });
    }

    [HttpPost("{libraryId}/books/import")]
    public async Task<IActionResult> MassiveBookImport([FromRoute] Guid libraryId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File non valido.");
        }

        // read the csv file
        // the file contains ISBN and it will be used to fetch book details from an external API (e.g., Open Library)

        using var reader = new StreamReader(file.OpenReadStream());
        foreach (var line in reader.ReadToEnd().Split(Environment.NewLine))
        {
            var isbn = line.Trim();
            if (string.IsNullOrWhiteSpace(isbn))
                continue;
            // Call external API to get book details by ISBN
            var bookDetails = await _isbnService.FetchBookDetails(isbn);
            if (bookDetails == null)
                continue;
            var bookEntity = new BookEntity
            {
                Id = Guid.NewGuid(),
                Title = bookDetails.Title,
                Author = bookDetails.Author,
                Description = bookDetails.Description,
                ISBN = isbn,
                LibraryId = libraryId,
                TotalCopies = 1
            };
            await _db.Books.AddAsync(bookEntity);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Restituisce le librerie entro un raggio (in metri) da un punto geografico.
    /// Usa ST_DWithin con geography per distanza sferica accurata in metri.
    /// </summary>
    [HttpGet("search/radius")]
    public async Task<IActionResult> SearchByRadius([FromQuery] SearchByRadiusDto dto)
    {
        if (dto.RadiusKilometers <= 0)
            return BadRequest("RadiusKilometers deve essere maggiore di zero.");

        var center = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var libraries = await _db.Libraries
            .Where(l => l.Location != null &&
                        EF.Functions.IsWithinDistance(l.Location, center, dto.RadiusKilometers * 1000, true))
            .ToListAsync();

        return Ok(_mapper.Map<List<LibraryDto>>(libraries));
    }

    /// <summary>
    /// Restituisce le librerie che si trovano all'interno di un poligono disegnato sulla mappa.
    /// Le coordinate devono essere in ordine (senso orario o antiorario); il poligono viene chiuso automaticamente.
    /// </summary>
    [HttpPost("search/polygon")]
    public async Task<IActionResult> SearchByPolygon([FromBody] SearchByPolygonDto dto)
    {
        if (dto.Coordinates.Count < 3)
            return BadRequest("Servono almeno 3 coordinate per definire un poligono.");

        // Converte le coordinate in Coordinate NTS (X = Longitude, Y = Latitude)
        var coords = dto.Coordinates
            .Select(c => new Coordinate(c.Longitude, c.Latitude))
            .ToList();

        // Chiude l'anello ripetendo il primo punto alla fine
        coords.Add(coords[0]);

        var polygon = _geometryFactory.CreatePolygon([.. coords]);

        var libraries = await _db.Libraries
            .Where(l => l.Location != null && polygon.Contains(l.Location))
            .ToListAsync();

        return Ok(_mapper.Map<List<LibraryDto>>(libraries));
    }




}
