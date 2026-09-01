namespace GeoLibrary.Server.Abstractions.Dtos.Dashboard;

/// <summary>
/// Riga della tabella dei libri più visualizzati nel periodo selezionato.
/// </summary>
public class TopBookDto
{
    public required Guid Id { get; set; }
    public required Guid LibraryId { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public required string LibraryName { get; set; }
    public required long ViewsCount { get; set; }
}
