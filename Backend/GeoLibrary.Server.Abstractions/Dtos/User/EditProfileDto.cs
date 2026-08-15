using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Dtos.User;

public class EditProfileDto
{
    public required string DisplayName { get; set; }
}
