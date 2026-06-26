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
    public required LibraryEntity LibraryEntity { get; set; }

    public required DateTime Date { get; set; }
    public long ViewsCount { get; set; } = 0;

}
