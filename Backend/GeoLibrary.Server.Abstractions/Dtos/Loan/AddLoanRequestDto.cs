namespace GeoLibrary.Server.Abstractions.Dtos.Loan;

public class AddLoanRequestDto
{
    public required Guid BookId { get; set; }
    public DateTime ReturnDate { get; set; }
}
