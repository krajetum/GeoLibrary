using NetTopologySuite.Geometries;

namespace GeoLibrary.Server.Abstractions.Entities;

public class LibraryEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; }
    public required string PostalCode { get; set; } = string.Empty;
    // Colonna geometrica PostGIS - (X=Longitude, Y=Latitude), SRID 4326 (WGS84)
    public required Point Location { get; set; }

    public ICollection<BooksEntity> Books { get; set; } = [];
}
