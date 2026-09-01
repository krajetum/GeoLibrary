using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Abstractions.Dtos.Dashboard;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Api.Controllers;

/// <summary>
/// Metriche aggregate della piattaforma. Riservate al ruolo di realm "admin":
/// a differenza delle statistiche di libreria/libro qui i dati non sono limitati
/// al patrimonio di un singolo utente.
/// </summary>
[Authorize(Policy = AuthPolicies.Admin)]
[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    /// <summary>Limite di righe restituibili da <see cref="GetTopBooks"/>.</summary>
    private const int MaxTopBooks = 50;

    private readonly GeoLibraryDbContext _db;

    public DashboardController(GeoLibraryDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Contatori complessivi: utenti, librerie, libri e richieste di prestito.
    /// </summary>
    [HttpGet("counters")]
    public async Task<IActionResult> GetCounters(CancellationToken cancellationToken)
    {
        var counters = new DashboardCountersDto
        {
            UsersCount = await _db.Users.AsNoTracking().LongCountAsync(cancellationToken),
            LibrariesCount = await _db.Libraries.AsNoTracking().LongCountAsync(cancellationToken),
            BooksCount = await _db.Books.AsNoTracking().LongCountAsync(cancellationToken),
            LoanRequestsCount = await _db.LoanRequests.AsNoTracking().LongCountAsync(cancellationToken),
        };

        return Ok(counters);
    }

    /// <summary>
    /// Visite giornaliere aggregate su tutte le librerie e su tutti i libri.
    /// </summary>
    [HttpGet("views")]
    public async Task<IActionResult> GetViews([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (!StatsExtensions.TryNormalizeRange(from, to, out var cleanedFrom, out var cleanedTo, out var error))
        {
            return BadRequest(error);
        }

        // Somma per giorno: un solo GROUP BY lato database per ciascuna delle due tabelle.
        var libraryViews = await _db.LibraryDailyViews
            .AsNoTracking()
            .Where(x => x.Date >= cleanedFrom && x.Date <= cleanedTo)
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateStats
            {
                Date = DateOnly.FromDateTime(g.Key),
                ViewsCount = g.Sum(x => x.ViewsCount)
            })
            .ToListAsync(cancellationToken);

        var bookViews = await _db.BookDailyViews
            .AsNoTracking()
            .Where(x => x.Date >= cleanedFrom && x.Date <= cleanedTo)
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateStats
            {
                Date = DateOnly.FromDateTime(g.Key),
                ViewsCount = g.Sum(x => x.ViewsCount)
            })
            .ToListAsync(cancellationToken);

        return Ok(new DashboardViewsDto
        {
            LibraryViews = StatsExtensions.FillStats(cleanedFrom, cleanedTo, libraryViews),
            BookViews = StatsExtensions.FillStats(cleanedFrom, cleanedTo, bookViews),
        });
    }

    /// <summary>
    /// Libri più visualizzati nel periodo, in ordine decrescente di visite.
    /// </summary>
    [HttpGet("top-books")]
    public async Task<IActionResult> GetTopBooks([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        if (!StatsExtensions.TryNormalizeRange(from, to, out var cleanedFrom, out var cleanedTo, out var error))
        {
            return BadRequest(error);
        }

        var cleanedLimit = Math.Clamp(limit, 1, MaxTopBooks);

        // Prima si aggrega (poche righe in uscita), poi si recuperano i dati descrittivi
        // dei soli libri selezionati: evita di caricare titoli e autori dell'intero catalogo.
        var totals = await _db.BookDailyViews
            .AsNoTracking()
            .Where(x => x.Date >= cleanedFrom && x.Date <= cleanedTo)
            .GroupBy(x => x.BookId)
            .Select(g => new { BookId = g.Key, ViewsCount = g.Sum(x => x.ViewsCount) })
            .OrderByDescending(x => x.ViewsCount)
            .Take(cleanedLimit)
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
        {
            return Ok(new List<TopBookDto>());
        }

        var bookIds = totals.Select(x => x.BookId).ToList();

        var books = await _db.Books
            .AsNoTracking()
            .Where(b => bookIds.Contains(b.Id))
            .Select(b => new
            {
                b.Id,
                b.LibraryId,
                b.Title,
                b.Author,
                LibraryName = b.Library.Name
            })
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        // L'ordinamento per visite viene dalla query aggregata, non dal dizionario.
        var result = totals
            .Where(t => books.ContainsKey(t.BookId))
            .Select(t =>
            {
                var book = books[t.BookId];
                return new TopBookDto
                {
                    Id = book.Id,
                    LibraryId = book.LibraryId,
                    Title = book.Title,
                    Author = book.Author,
                    LibraryName = book.LibraryName,
                    ViewsCount = t.ViewsCount,
                };
            })
            .ToList();

        return Ok(result);
    }
}
