using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Dtos.User;

public class ProfileDto
{
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    /// <summary>URL firmato dell'avatar, generato a ogni richiesta. Null se non caricato.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>URL firmato della miniatura da 400px, per l'avatar piccolo nella barra.</summary>
    public string? AvatarThumbnailUrl { get; set; }
}
