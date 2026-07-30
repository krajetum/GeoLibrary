namespace GeoLibrary.Server.Abstractions.Dtos.Book;

public class GetBooksQueryDto
{
    public int Page { get; set; } = 1;
    public int ItemsPerPage { get; set; } = 10;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}
