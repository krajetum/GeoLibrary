using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Dtos.Book;

public class BookDto 
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Author { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public required Guid LibraryId { get; set; }

    public bool IsHidden { get; set; }

    public string? CoverImageUrl { get; set; }
    public string? CoverThumbnailUrl { get; set; }

    /// <summary>
    /// True se l'utente autenticato possiede la libreria che contiene il libro.
    /// Come per LibraryDto serve solo a pilotare la UI.
    /// </summary>
    public bool IsAdmin { get; set; } = false;
}
