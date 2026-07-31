namespace GeoLibrary.Server.Abstractions.Dtos.Book;

/// <summary>
/// Libro trovato dalla ricerca per area: oltre ai dati del libro porta
/// nome e posizione della libreria che lo contiene, per mostrare i pin sulla mappa.
/// La distanza dal centro della ricerca la calcola il client, che il centro ce l'ha già.
/// </summary>
public class BookSearchResultDto
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }

    public required Guid LibraryId { get; set; }
    public required string LibraryName { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string? CoverThumbnailUrl { get; set; }
}
