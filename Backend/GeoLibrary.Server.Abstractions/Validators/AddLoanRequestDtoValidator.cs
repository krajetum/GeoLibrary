using FluentValidation;
using GeoLibrary.Server.Abstractions.Dtos.Loan;

namespace GeoLibrary.Server.Abstractions.Validators;

public class AddLoanRequestDtoValidator : AbstractValidator<AddLoanRequestDto>
{
    public AddLoanRequestDtoValidator()
    {
        RuleFor(x => x.BookId).NotEmpty().WithMessage("BookId is required.");
        RuleFor(x => x.ReturnDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Return date must be in the future.");
        RuleFor(x => x.ReturnDate)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(90))
            .WithMessage("Return date cannot be more than 90 days from now.");
    }
}
