namespace GeoLibrary.Server.Abstractions.Dtos.Library;

public class CoordinateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class SearchByPolygonDto
{
    /// <summary>
    /// Lista di coordinate che definiscono il poligono.
    /// Minimo 3 punti; l'anello viene chiuso automaticamente.
    /// </summary>
    public List<CoordinateDto> Coordinates { get; set; } = [];

    /// <summary>
    /// Testo opzionale da cercare (titolo, autore o ISBN) fra i risultati dell'area.
    /// Usato solo dalla ricerca dei libri.
    /// </summary>
    public string? Search { get; set; }
}
