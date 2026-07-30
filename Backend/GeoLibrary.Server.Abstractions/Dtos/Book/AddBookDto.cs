namespace GeoLibrary.Server.Abstractions.Dtos.Book;

public class AddBookDto
{
    public required Guid LibraryId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Author { get; set; }
    public string ISBN { get; set; } = string.Empty;

    // Non required: altrimenti a rifiutare la richiesta sarebbe il deserializzatore, non il validator.
    public int TotalCopies { get; set; }
    public bool IsHidden { get; set; }
}
