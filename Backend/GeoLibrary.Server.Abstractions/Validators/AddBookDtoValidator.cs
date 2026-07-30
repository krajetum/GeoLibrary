using FluentValidation;
using GeoLibrary.Server.Abstractions.Dtos.Book;

namespace GeoLibrary.Server.Abstractions.Validators;

public class AddBookDtoValidator : AbstractValidator<AddBookDto>
{
    public AddBookDtoValidator()
    {
        RuleFor(x => x.LibraryId).NotEmpty().WithMessage("LibraryId is required.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
        RuleFor(x => x.Author).NotEmpty().WithMessage("Author is required.");
        RuleFor(x => x.TotalCopies).GreaterThanOrEqualTo(1).WithMessage("Total copies must be at least 1.");
    }
}
