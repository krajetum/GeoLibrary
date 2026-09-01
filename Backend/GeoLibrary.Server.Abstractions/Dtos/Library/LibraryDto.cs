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
    /// <summary>
    /// Indirizzo e CAP sono nulli per chi non ha diritto alla posizione esatta:
    /// restano solo città e nazione. Vedi IsApproximateLocation.
    /// </summary>
    public string? Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public required double Latitude { get; set; } = 0.0;
    public required double Longitude { get; set; } = 0.0;
    public required long BookCount { get; set; } = 0;

    /// <summary>
    /// Numero di visualizzazioni. Valorizzato solo per il proprietario:
    /// null significa "non hai diritto a questo dato", non "zero visite".
    /// </summary>
    public long? ViewsCount { get; set; }

    public bool IsHidden { get; set; }

    /// <summary>
    /// True quando le coordinate sono state arrotondate e l'indirizzo omesso.
    /// Serve all'interfaccia per dichiararlo all'utente.
    /// </summary>
    public bool IsApproximateLocation { get; set; }

    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// True se l'utente autenticato è il proprietario della libreria (può modificarla).
    /// Calcolato lato server; solo per pilotare la UI, l'autorizzazione vera è sugli endpoint di scrittura.
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}
