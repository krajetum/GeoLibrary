namespace GeoLibrary.Server.Abstractions.Entities;

public class BooksEntity
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Author { get; set; }

    public required Guid LibraryId { get; set; }
    public required LibraryEntity Library { get; set; }
}
