namespace GeoLibrary.Server.Abstractions.Dtos;

public class PagedResultDto<T>
{
    public required IReadOnlyList<T> Items { get; set; }
    public required int TotalCount { get; set; }
}
