using GeoLibrary.Server.Abstractions.Dtos.Loan;
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
public class LoanController : ControllerBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly GeoLibraryDbContext _db;
    private readonly ILogger<LoanController> _logger;

    public LoanController(IHttpContextAccessor contextAccessor, GeoLibraryDbContext db, ILogger<LoanController> logger)
    {
        _contextAccessor = contextAccessor;
        _db = db;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddLoanRequest(AddLoanRequestDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(loanDto);

        var validator = new AddLoanRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(loanDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var book = await _db.Books.Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == loanDto.BookId);
        if (book is null)
        {
            return NotFound();
        }

        // Nascosto il libro o l'intera libreria: non si può chiedere in prestito
        // qualcosa che per l'utente non esiste.
        if (book.IsHidden || book.Library.IsHidden)
        {
            return NotFound();
        }

        if (book.Library.UserId == userId)
        {
            return Forbid();
        }

        var alreadyRequested = await _db.LoanRequests.AnyAsync(x =>
            x.BookId == book.Id &&
            x.UserId == userId &&
            (x.Status == LoanRequestStatus.Pending || x.Status == LoanRequestStatus.Approved));

        if (alreadyRequested)
        {
            return Conflict("Esiste già una richiesta in corso per questo libro.");
        }

        var entity = new LoanRequestEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BookId = book.Id,
            BookingDate = DateTime.UtcNow,
            // La colonna è "timestamp with time zone": Npgsql rifiuta le date che non
            // sono marcate come UTC, e quella deserializzata dal JSON è Unspecified.
            ReturnDate = DateTime.SpecifyKind(loanDto.ReturnDate, DateTimeKind.Utc),
            Status = LoanRequestStatus.Pending
        };

        await _db.LoanRequests.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Ok(await ToDto(entity));
    }

    /// <summary>
    /// Richieste di prestito di un libro. Il proprietario della libreria le vede tutte,
    /// chiunque altro vede solo le proprie.
    /// </summary>
    [HttpGet("book/{bookId}")]
    public async Task<IActionResult> GetBookLoanRequests([FromRoute] Guid bookId)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var book = await _db.Books.AsNoTracking().Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return NotFound();
        }

        var isOwner = book.Library.UserId == userId;

        var loans = _db.LoanRequests.AsNoTracking().Where(x => x.BookId == bookId);
        if (!isOwner)
        {
            loans = loans.Where(x => x.UserId == userId);
        }

        // Join esplicita: LoanRequestEntity non ha una navigation property verso l'utente.
        var result = await (from loan in loans
                            join user in _db.Users on loan.UserId equals user.Id
                            orderby loan.BookingDate descending
                            select new LoanRequestDto
                            {
                                Id = loan.Id,
                                BookId = loan.BookId,
                                UserId = loan.UserId,
                                UserDisplayName = user.DisplayName,
                                BookingDate = loan.BookingDate,
                                ReturnDate = loan.ReturnDate,
                                Status = loan.Status.ToString(),
                                BookTitle = book.Title,
                                LibraryId = book.LibraryId,
                                LibraryName = book.Library.Name
                            }).ToListAsync();

        return Ok(result);
    }

    /// <summary>
    /// Le richieste fatte dall'utente corrente, su qualsiasi libreria.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyLoanRequests()
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await (from loan in _db.LoanRequests.AsNoTracking()
                            join book in _db.Books on loan.BookId equals book.Id
                            join library in _db.Libraries on book.LibraryId equals library.Id
                            join user in _db.Users on loan.UserId equals user.Id
                            where loan.UserId == userId
                            orderby loan.BookingDate descending
                            select new LoanRequestDto
                            {
                                Id = loan.Id,
                                BookId = loan.BookId,
                                UserId = loan.UserId,
                                UserDisplayName = user.DisplayName,
                                BookingDate = loan.BookingDate,
                                ReturnDate = loan.ReturnDate,
                                Status = loan.Status.ToString(),
                                BookTitle = book.Title,
                                LibraryId = library.Id,
                                LibraryName = library.Name
                            }).ToListAsync();

        return Ok(result);
    }

    /// <summary>
    /// Le richieste ricevute sui libri delle librerie di cui l'utente è proprietario.
    /// </summary>
    [HttpGet("received")]
    public async Task<IActionResult> GetReceivedLoanRequests()
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        // Stessa query di GetMyLoanRequests, ma il filtro è sul proprietario della libreria.
        var result = await (from loan in _db.LoanRequests.AsNoTracking()
                            join book in _db.Books on loan.BookId equals book.Id
                            join library in _db.Libraries on book.LibraryId equals library.Id
                            join user in _db.Users on loan.UserId equals user.Id
                            where library.UserId == userId
                            orderby loan.BookingDate descending
                            select new LoanRequestDto
                            {
                                Id = loan.Id,
                                BookId = loan.BookId,
                                UserId = loan.UserId,
                                UserDisplayName = user.DisplayName,
                                BookingDate = loan.BookingDate,
                                ReturnDate = loan.ReturnDate,
                                Status = loan.Status.ToString(),
                                BookTitle = book.Title,
                                LibraryId = library.Id,
                                LibraryName = library.Name
                            }).ToListAsync();

        return Ok(result);
    }

    // Il vincolo :guid evita che questa rotta catturi anche /loan/mine e /loan/received.
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateLoanStatus([FromRoute] Guid id, UpdateLoanStatusDto statusDto)
    {
        ArgumentNullException.ThrowIfNull(statusDto);

        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<LoanRequestStatus>(statusDto.Status, out var newStatus))
        {
            return BadRequest("Stato non riconosciuto.");
        }

        var loan = await _db.LoanRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (loan is null)
        {
            return NotFound();
        }

        var book = await _db.Books.Include(b => b.Library).FirstOrDefaultAsync(b => b.Id == loan.BookId);
        if (book is null)
        {
            return NotFound();
        }

        // Lo stato lo cambia solo chi presta il libro.
        if (book.Library.UserId != userId)
        {
            return Forbid();
        }

        if (!IsTransitionAllowed(loan.Status, newStatus))
        {
            return BadRequest($"Non si può passare da {loan.Status} a {newStatus}.");
        }

        if (newStatus == LoanRequestStatus.Approved)
        {
            var activeLoans = await _db.LoanRequests.CountAsync(x =>
                x.BookId == book.Id && x.Status == LoanRequestStatus.Approved);

            if (activeLoans >= book.TotalCopies)
            {
                return BadRequest("Tutte le copie sono già in prestito.");
            }
        }

        loan.Status = newStatus;
        await _db.SaveChangesAsync();

        return Ok(await ToDto(loan));
    }

    private static bool IsTransitionAllowed(LoanRequestStatus current, LoanRequestStatus next)
    {
        return (current, next) switch
        {
            (LoanRequestStatus.Pending, LoanRequestStatus.Approved) => true,
            (LoanRequestStatus.Pending, LoanRequestStatus.Rejected) => true,
            (LoanRequestStatus.Approved, LoanRequestStatus.Returned) => true,
            _ => false
        };
    }

    private async Task<LoanRequestDto> ToDto(LoanRequestEntity loan)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == loan.UserId);
        var book = await _db.Books.AsNoTracking().Include(b => b.Library)
            .FirstOrDefaultAsync(x => x.Id == loan.BookId);

        return new LoanRequestDto
        {
            Id = loan.Id,
            BookId = loan.BookId,
            UserId = loan.UserId,
            UserDisplayName = user?.DisplayName ?? string.Empty,
            BookingDate = loan.BookingDate,
            ReturnDate = loan.ReturnDate,
            Status = loan.Status.ToString(),
            BookTitle = book?.Title ?? string.Empty,
            LibraryId = book?.LibraryId ?? Guid.Empty,
            LibraryName = book?.Library.Name ?? string.Empty
        };
    }
}
