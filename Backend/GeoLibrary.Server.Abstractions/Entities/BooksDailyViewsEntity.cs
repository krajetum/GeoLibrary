using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class BookDailyViewEntity
{
    public required Guid BookId { get; set; }
    /// <summary>
    /// Navigation property to the associated BookEntity.
    /// </summary>
    public BookEntity Book { get; set; } = null!;

    public required DateTime Date { get; set; }
    public long ViewsCount { get; set; } = 0;

    
}
