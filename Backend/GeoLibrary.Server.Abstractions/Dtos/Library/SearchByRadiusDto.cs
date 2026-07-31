namespace GeoLibrary.Server.Abstractions.Dtos.Library;

public class SearchByRadiusDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Radius in kilometers.</summary>
    public double RadiusKilometers { get; set; }

    /// <summary>
    /// Testo opzionale da cercare (titolo, autore o ISBN) fra i risultati dell'area.
    /// Usato solo dalla ricerca dei libri.
    /// </summary>
    public string? Search { get; set; }
}
