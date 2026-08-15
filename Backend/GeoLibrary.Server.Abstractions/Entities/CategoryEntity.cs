namespace GeoLibrary.Server.Abstractions.Entities;

/// <summary>
/// Categoria di libro. La lista è fissa: viene creata dal seeding e non è modificabile via API.
/// </summary>
public class CategoryEntity
{
    public required Guid Id { get; set; }
    /// <summary>
    /// Chiave stabile in inglese, usata anche per agganciare i "subjects" di OpenLibrary.
    /// </summary>
    public required string Slug { get; set; }
    public required string Name { get; set; }

    public ICollection<BookEntity> Books { get; set; } = [];
}
