namespace GeoLibrary.Server.Abstractions.Entities;

public class BookEntity
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Author { get; set; }

    public required Guid LibraryId { get; set; }
    public required LibraryEntity Library { get; set; }

    public int TotalCopies { get; set; } = 0;

    public int ViewsCount { get; set; } = 0;

    // Navigation property for loaning
    public ICollection<LoanRequestEntity> Bookings { get; set; } = [];
    public ICollection<BookDailyViewEntity> DailyViews { get; set; } = [];
}
