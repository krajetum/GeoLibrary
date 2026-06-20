using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class Library
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string CountryCode { get; set; }
    public required string PostalCode { get; set; } = string.Empty;
    public required double Latitude { get; set; } = 0.0;
    public required double Longitude { get; set; } = 0.0;

    public ICollection<Books> Books { get; set; } = [];
}
