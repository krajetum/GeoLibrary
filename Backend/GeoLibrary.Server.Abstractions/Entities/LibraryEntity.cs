using NetTopologySuite.Geometries;

namespace GeoLibrary.Server.Abstractions.Entities;

public class LibraryEntity
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; }
    public required string PostalCode { get; set; } = string.Empty;
    // Colonna geometrica PostGIS - (X=Longitude, Y=Latitude), SRID 4326 (WGS84)
    public required Point Location { get; set; }

    public string? ImageKey { get; set; } // chiave dell'immagine su MinIO

    /// <summary>
    /// Se true la libreria è visibile solo al proprietario e non compare nelle ricerche.
    /// </summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Statistics only, denormalized for performance. For better granularity, see 
    /// </summary>
    public int ViewsCount { get; set; } = 0;

    public ICollection<BookEntity> Books { get; set; } = [];
    public ICollection<LibraryDailyViewEntity> DailyViews { get; set; } = [];
}
