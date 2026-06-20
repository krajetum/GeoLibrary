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
}
