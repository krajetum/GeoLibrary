using AutoMapper;
using AutoMapper.QueryableExtensions;
using GeoLibrary.Server.Abstractions.Dtos;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Models;
using GeoLibrary.Server.Abstractions.Services;
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
    private readonly IStorageService _storageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ITrackingService<BookTrackingRequest> _bookTrackingService;
    private readonly ITrackingService<LibraryTrackingRequest> _libraryTrackingService;
    private readonly ILogger<LibraryController> _logger;

    private static readonly GeometryFactory _geometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public LibraryController(
        GeoLibraryDbContext db, 
        IMapper mapper, 
        ISBNService isbnService, 
        IStorageService storageService, 
        IHttpClientFactory httpClientFactory, 
        IHttpContextAccessor httpContextAccessor, 
        ITrackingService<BookTrackingRequest> bookTrackingService,
        ITrackingService<LibraryTrackingRequest> libraryTrackingService,
        ILogger<LibraryController> logger)
    {
        _db = db;
        _mapper = mapper;
        _isbnService = isbnService;
        _storageService = storageService;
        _httpClientFactory = httpClientFactory;
        _contextAccessor = httpContextAccessor;
        _bookTrackingService = bookTrackingService;
        _libraryTrackingService = libraryTrackingService;
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

        // Si restituisce l'entità e non il dto in ingresso: al client serve l'Id per caricare l'immagine.
        return Ok(_mapper.Map<LibraryDto>(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLibrary([FromRoute] Guid id, AddLibraryDto libraryDto)
    {
        ArgumentNullException.ThrowIfNull(libraryDto);

        var validator = new AddLibraryDtoValidator();
        var validationResult = await validator.ValidateAsync(libraryDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var entity = await _db.Libraries.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        if (entity.UserId != userId)
        {
            return Forbid();
        }

        // Il dto non ha Id, UserId e ImageKey, quindi la mappatura sull'entità esistente li lascia intatti.
        _mapper.Map(libraryDto, entity);
        entity.Location = _geometryFactory.CreatePoint(
            new Coordinate(libraryDto.Longitude, libraryDto.Latitude));

        await _db.SaveChangesAsync();

        return Ok(await ToDto(entity, exactLocation: true));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLibrary([FromRoute] Guid id)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var library = await _db.Libraries.FirstOrDefaultAsync(x => x.Id == id);
        if (library is null)
        {
            return NotFound();
        }

        if (library.UserId != userId)
        {
            return Forbid();
        }

        // Le FK sono in cascata: libri, prestiti e statistiche li cancella il database.
        // Le immagini su MinIO restano invece orfane, non c'è pulizia dello storage.
        _db.Libraries.Remove(library);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/image")]
    public async Task<IActionResult> UploadImage([FromRoute] Guid id, IFormFile file)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var library = await _db.Libraries.FirstOrDefaultAsync(x => x.Id == id);
        if (library is null)
        {
            return NotFound();
        }

        if (library.UserId != userId)
        {
            return Forbid();
        }

        using var stream = file.OpenReadStream();
        var key = await _storageService.UploadImageAsync(stream, file.FileName, file.ContentType);
        library.ImageKey = key;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            ImageUrl = await _storageService.GetUrl(key),
            ThumbnailUrl = await _storageService.GetThumbnailUrl(key)
        });
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

        var result = new List<LibraryDto>();
        foreach (var x in libraries)
        {
            // Sono le librerie dell'utente: la posizione esatta è sempre la sua.
            var dto = await ToDto(x.Entity, exactLocation: true);
            dto.BookCount = x.BookCount;
            result.Add(dto);
        }

        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLibrary([FromRoute] Guid id)
    {
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

        // Se è nascosta, per chi non è il proprietario semplicemente non esiste.
        if (result.Entity.IsHidden && result.Entity.UserId != userId)
        {
            return NotFound();
        }

        var isOwner = result.Entity.UserId == userId;
        var withApprovedLoan = await LibrariesWithApprovedLoan(userId);

        var dto = await ToDto(result.Entity, isOwner || withApprovedLoan.Contains(result.Entity.Id));
        dto.BookCount = result.BookCount;
        dto.IsAdmin = isOwner;
        // Il numero di visite è un dato di analisi del proprietario: non va esposto ai visitatori.
        dto.ViewsCount = isOwner ? result.Entity.ViewsCount : null;

        if (!isOwner && _contextAccessor.TryGetUserSignature(out string userSignature))
        {
            await _libraryTrackingService.TrackAsync(new LibraryTrackingRequest()
            {
                LibraryId = id,
                UserSignature = userSignature,
            });
        }

        return Ok(dto);
    }

    [HttpGet("{libraryId}/stats")]
    public async Task<IActionResult> GetLibraryStats([FromRoute] Guid libraryId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var library = await _db.Libraries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == libraryId);
        if (library is null)
        {
            return NotFound();
        }
        // Solo il proprietario della libreria può vedere le statistiche.
        if (library.UserId != userId)
        {
            return Forbid();
        }
        if (!StatsExtensions.TryNormalizeRange(from, to, out var cleanedFrom, out var cleanedTo, out var error))
        {
            return BadRequest(error);
        }

        var stats = await _db.LibraryDailyViews
            .AsNoTracking()
            .Where(x => x.LibraryId == libraryId && x.Date >= cleanedFrom && x.Date <= cleanedTo)
            .OrderBy(x => x.Date)
            .Select(x => new DateStats
            {
                Date = DateOnly.FromDateTime(x.Date),
                ViewsCount = x.ViewsCount
            })
            .ToListAsync();

        var completeStats = StatsExtensions.FillStats(cleanedFrom, cleanedTo, stats);

        return Ok(completeStats);

    }

    [AllowAnonymous]
    [HttpGet("{libraryId}/books")]
    public async Task<IActionResult> GetLibraryBooks([FromRoute] Guid libraryId, [FromQuery] GetBooksQueryDto query)
    {
        _contextAccessor.TryGetUserId(out var userId);

        var library = await _db.Libraries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == libraryId);
        if (library is null)
        {
            return NotFound();
        }

        var isOwner = library.UserId == userId;
        if (library.IsHidden && !isOwner)
        {
            return NotFound();
        }

        var page = query.Page < 1 ? 1 : query.Page;

        var booksQuery = _db.Books
            .AsNoTracking()
            .Where(x => x.LibraryId == libraryId);

        // Il filtro va prima del CountAsync, altrimenti l'ultima pagina della tabella resta vuota.
        if (!isOwner)
        {
            booksQuery = booksQuery.Where(x => !x.IsHidden);
        }

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

        // In elenco basta la miniatura. Le chiavi si leggono in una query sola, non una per libro.
        var ids = items.Select(x => x.Id).ToList();
        var covers = await _db.Books
            .Where(x => ids.Contains(x.Id) && x.CoverImageKey != "")
            .ToDictionaryAsync(x => x.Id, x => x.CoverImageKey);

        foreach (var item in items)
        {
            if (covers.TryGetValue(item.Id, out var key))
            {
                item.CoverThumbnailUrl = await _storageService.GetThumbnailUrl(key);
            }
        }

        return Ok(new PagedResultDto<BookDto> { Items = items, TotalCount = totalCount });
    }

    [AllowAnonymous]
    [HttpGet("{libraryId}/books/{bookId}")]
    public async Task<IActionResult> GetBook([FromRoute] Guid libraryId, [FromRoute] Guid bookId)
    {

        _contextAccessor.TryGetUserId(out var userId);

        var book = await _db.Books.AsNoTracking()
            .Include(x => x.Library)
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.LibraryId == libraryId && x.Id == bookId);

        if (book is null)
        {
            return NotFound();
        }

        // Nascosto il libro o l'intera libreria: per gli altri utenti non esiste.
        var isOwner = book.Library.UserId == userId;
        if (!isOwner && (book.IsHidden || book.Library.IsHidden))
        {
            return NotFound();
        }

        var dto = _mapper.Map<BookDto>(book);
        dto.IsAdmin = isOwner;
        // Come per la libreria: le visite le vede solo chi possiede il patrimonio.
        dto.ViewsCount = isOwner ? book.ViewsCount : null;

        if (!string.IsNullOrEmpty(book.CoverImageKey))
        {
            dto.CoverImageUrl = await _storageService.GetUrl(book.CoverImageKey);
            dto.CoverThumbnailUrl = await _storageService.GetThumbnailUrl(book.CoverImageKey);
        }

        if (!isOwner && _contextAccessor.TryGetUserSignature(out string userSignature))
        {
            await _bookTrackingService.TrackAsync(new BookTrackingRequest()
            {
                LibraryId = libraryId,
                BookId = bookId,
                UserSignature = userSignature,
            });
        }

        return Ok(dto);
    }

    [HttpGet("{libraryId}/books/{bookId}/stats")]
    public async Task<IActionResult> GetBookStats([FromRoute] Guid libraryId, [FromRoute] Guid bookId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }
        var book = await _db.Books.AsNoTracking()
            .Include(x => x.Library)
            .FirstOrDefaultAsync(x => x.LibraryId == libraryId && x.Id == bookId);

        if (book is null)
        {
            return NotFound();
        }
        // Solo il proprietario della libreria può vedere le statistiche.
        if (book.Library.UserId != userId)
        {
            return Forbid();
        }

        if (!StatsExtensions.TryNormalizeRange(from, to, out var cleanedFrom, out var cleanedTo, out var error))
        {
            return BadRequest(error);
        }

        var stats = await _db.BookDailyViews
            .AsNoTracking()
            .Where(x => x.LibraryId == libraryId && x.BookId == bookId && x.Date >= cleanedFrom && x.Date <= cleanedTo)
            .OrderBy(x => x.Date)
            .Select(x => new DateStats
            {
                Date = DateOnly.FromDateTime(x.Date),
                ViewsCount = x.ViewsCount
            })
            .ToListAsync();

        // Se ci sono buchi di date dove non ci sono state delle visite, li riempiamo con ViewsCount = 0.
        var completeStats = StatsExtensions.FillStats(cleanedFrom, cleanedTo, stats);

        return Ok(completeStats);
    }

    [HttpPost("{libraryId}/books/import")]
    public async Task<IActionResult> MassiveBookImport([FromRoute] Guid libraryId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File non valido.");
        }

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var library = await _db.Libraries.FirstOrDefaultAsync(x => x.Id == libraryId);
        if (library is null)
        {
            return NotFound();
        }

        if (library.UserId != userId)
        {
            return Forbid();
        }

        // Un ISBN per riga: titolo, autore e copertina arrivano da OpenLibrary.
        // Split su '\n' e non su Environment.NewLine: il file può avere fine riga Windows o Unix.
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();

        var imported = 0;
        var skipped = 0;

        // Le categorie sono poche e fisse: si leggono una volta sola e si abbinano in memoria.
        var allCategories = await _db.Categories.ToListAsync();

        foreach (var line in content.Split('\n'))
        {
            var isbn = line.Trim();
            if (string.IsNullOrWhiteSpace(isbn))
                continue;

            var bookDetails = await _isbnService.FetchBookDetails(isbn);
            if (bookDetails is null)
            {
                skipped++;
                continue;
            }

            var bookEntity = new BookEntity
            {
                Id = Guid.NewGuid(),
                Title = bookDetails.Title,
                Author = bookDetails.Author,
                Description = bookDetails.Description,
                ISBN = isbn,
                LibraryId = libraryId,
                TotalCopies = 1,
                PublishedAt = bookDetails.PublishedDate,
                CoverImageKey = await DownloadCover(bookDetails.CoverUrl, isbn)
            };

            // I "subjects" di OpenLibrary sono testo libero ("Fiction, fantasy, epic"): oltre allo
            // slug intero si guardano anche le singole parole, altrimenti non aggancia quasi nulla.
            var slugs = bookDetails.Categories
                .SelectMany(x => x.Slug.Split('-', StringSplitOptions.RemoveEmptyEntries).Append(x.Slug))
                .ToHashSet();

            bookEntity.Categories = allCategories.Where(c => slugs.Contains(c.Slug)).ToList();

            await _db.Books.AddAsync(bookEntity);
            imported++;
        }

        await _db.SaveChangesAsync();

        return Ok(new { Imported = imported, Skipped = skipped });
    }

    /// <summary>
    /// Scarica la copertina indicata da OpenLibrary e la carica come quelle manuali.
    /// Restituisce la chiave, o stringa vuota se il libro non ha copertina.
    /// </summary>
    private async Task<string> DownloadCover(string coverUrl, string isbn)
    {
        if (string.IsNullOrEmpty(coverUrl))
            return string.Empty;

        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(coverUrl);
        if (!response.IsSuccessStatusCode)
            return string.Empty;

        // La risposta HTTP non è riavvolgibile, mentre UploadImage rilegge lo stream per la miniatura.
        using var image = new MemoryStream();
        await response.Content.CopyToAsync(image);
        image.Position = 0;

        return await _storageService.UploadImageAsync(image, $"{isbn}.jpg", "image/jpeg");
    }

    /// <summary>
    /// Restituisce le librerie entro un raggio (in metri) da un punto geografico.
    /// Usa ST_DWithin con geography per distanza sferica accurata in metri.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("search/radius")]
    public async Task<IActionResult> SearchByRadius([FromQuery] SearchByRadiusDto dto)
    {
        if (dto.RadiusKilometers <= 0)
            return BadRequest("RadiusKilometers deve essere maggiore di zero.");

        var center = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        // Le ricerche servono a scoprire librerie altrui: le nascoste non compaiono a nessuno.
        var libraries = await _db.Libraries
            .Where(l => !l.IsHidden && l.Location != null &&
                        EF.Functions.IsWithinDistance(l.Location, center, dto.RadiusKilometers * 1000, true))
            .ToListAsync();

        return Ok(await ToSearchResults(libraries));
    }

    /// <summary>
    /// Restituisce le librerie che si trovano all'interno di un poligono disegnato sulla mappa.
    /// Le coordinate devono essere in ordine (senso orario o antiorario); il poligono viene chiuso automaticamente.
    /// </summary>
    [AllowAnonymous]
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
            .Where(l => !l.IsHidden && l.Location != null && polygon.Contains(l.Location))
            .ToListAsync();

        return Ok(await ToSearchResults(libraries));
    }

    /// <summary>
    /// Mappa le librerie trovate da una ricerca, ognuna con il dettaglio di posizione
    /// a cui il chiamante ha diritto.
    /// </summary>
    private async Task<List<LibraryDto>> ToSearchResults(List<LibraryEntity> libraries)
    {
        _contextAccessor.TryGetUserId(out var userId);
        var withApprovedLoan = await LibrariesWithApprovedLoan(userId);

        var result = new List<LibraryDto>();
        foreach (var library in libraries)
        {
            var isOwner = library.UserId == userId;
            var dto = await ToDto(library, isOwner || withApprovedLoan.Contains(library.Id));
            dto.IsAdmin = isOwner;
            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Librerie da cui l'utente ha un prestito approvato in corso: sono quelle di cui
    /// può vedere l'indirizzo, perché deve andarci a ritirare il libro.
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

    /// <summary>
    /// Mappa la libreria aggiungendo le URL dell'immagine, che sono firmate e scadono
    /// dopo un'ora: vanno quindi rigenerate a ogni risposta.
    /// Con exactLocation a false l'indirizzo viene omesso e le coordinate arrotondate.
    /// </summary>
    private async Task<LibraryDto> ToDto(LibraryEntity library, bool exactLocation)
    {
        var dto = _mapper.Map<LibraryDto>(library);

        if (!exactLocation)
        {
            dto.Address = null;
            dto.PostalCode = null;
            // Si arrotonda invece di spostare il punto: i decimali successivi
            // spariscono dalla risposta e non c'è nessuno scostamento da sottrarre
            // per chi conoscesse l'algoritmo. Due decimali valgono circa un chilometro.
            dto.Latitude = Math.Round(dto.Latitude, 2);
            dto.Longitude = Math.Round(dto.Longitude, 2);
            dto.IsApproximateLocation = true;
        }

        if (!string.IsNullOrEmpty(library.ImageKey))
        {
            dto.ImageUrl = await _storageService.GetUrl(library.ImageKey);
            dto.ThumbnailUrl = await _storageService.GetThumbnailUrl(library.ImageKey);
        }

        return dto;
    }
}
