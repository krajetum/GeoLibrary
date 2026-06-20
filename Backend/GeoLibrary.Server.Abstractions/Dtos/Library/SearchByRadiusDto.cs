namespace GeoLibrary.Server.Abstractions.Dtos.Library;

public class SearchByRadiusDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Radius in kilometers.</summary>
    public double RadiusKilometers { get; set; }
}
