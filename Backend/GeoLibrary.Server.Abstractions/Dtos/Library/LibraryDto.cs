using GeoLibrary.Server.Abstractions.Dtos.Book;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Dtos.Library;

public class LibraryDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; }
    public required string PostalCode { get; set; } = string.Empty;
    public required double Latitude { get; set; } = 0.0;
    public required double Longitude { get; set; } = 0.0;
    public required long BookCount { get; set; } = 0;
    public bool IsHidden { get; set; }

    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// True se l'utente autenticato è il proprietario della libreria (può modificarla).
    /// Calcolato lato server; solo per pilotare la UI, l'autorizzazione vera è sugli endpoint di scrittura.
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}
