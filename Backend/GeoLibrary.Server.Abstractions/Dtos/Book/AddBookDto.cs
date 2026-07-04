namespace GeoLibrary.Server.Abstractions.Dtos.Book;

public class AddBookDto
{
    public required Guid LibraryId { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Author { get; set; }
    public string ISBN { get; set; } = string.Empty;
}
