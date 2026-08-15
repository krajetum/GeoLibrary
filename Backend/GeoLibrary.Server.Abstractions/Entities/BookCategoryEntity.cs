namespace GeoLibrary.Server.Abstractions.Entities;

/// <summary>
/// Tabella di join tra libri e categorie.
/// </summary>
public class BookCategoryEntity
{
    public required Guid BookId { get; set; }
    public required Guid CategoryId { get; set; }
}
