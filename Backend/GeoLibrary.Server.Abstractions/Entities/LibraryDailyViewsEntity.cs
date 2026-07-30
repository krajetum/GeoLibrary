using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class LibraryDailyViewEntity
{
    public required Guid LibraryId { get; set; }
    /// <summary>
    /// Navigation property to the associated LibraryEntity.
    /// </summary>
    public LibraryEntity LibraryEntity { get; set; } = null!;

    public required DateTime Date { get; set; }
    public long ViewsCount { get; set; } = 0;

}
