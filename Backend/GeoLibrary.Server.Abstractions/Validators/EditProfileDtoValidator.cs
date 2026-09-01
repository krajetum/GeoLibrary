using FluentValidation;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Dtos.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Validators;

public class EditProfileDtoValidator : AbstractValidator<EditProfileDto>
{

    public EditProfileDtoValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100).WithMessage("Display name is required and must not exceed 100 characters.");
    }


}