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
    public required Guid LibraryId { get; set; }
}
