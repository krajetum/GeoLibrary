namespace GeoLibrary.Server.Abstractions.Dtos.Loan;

public class LoanRequestDto
{
    public required Guid Id { get; set; }
    public required Guid BookId { get; set; }
    public required Guid UserId { get; set; }

    /// <summary>
    /// Nome del richiedente, mostrato al proprietario della libreria.
    /// Solo il nome visualizzato: l'email non esce mai da qui.
    /// </summary>
    public required string UserDisplayName { get; set; }

    public required DateTime BookingDate { get; set; }
    public required DateTime ReturnDate { get; set; }

    // Stringa e non LoanRequestStatus: l'enum verrebbe serializzato come intero,
    // mentre il frontend ragiona sulle etichette (Pending, Approved, ...).
    public required string Status { get; set; }

    // Contesto del libro: serve agli elenchi trasversali (/loan/mine, /loan/received),
    // dove la pagina non sa già di quale libro si sta parlando.
    public string BookTitle { get; set; } = string.Empty;
    public Guid LibraryId { get; set; }
    public string LibraryName { get; set; } = string.Empty;
}
