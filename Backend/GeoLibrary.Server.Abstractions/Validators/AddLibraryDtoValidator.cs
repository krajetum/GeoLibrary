using FluentValidation;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Validators;

public class AddLibraryDtoValidator : AbstractValidator<AddLibraryDto>
{

    public AddLibraryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Library name is required.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.");
        RuleFor(x => x.CountryCode).NotEmpty().WithMessage("Country code is required.");
        RuleFor(x => x.PostalCode).NotEmpty().WithMessage("Postal code is required.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
    }


}
