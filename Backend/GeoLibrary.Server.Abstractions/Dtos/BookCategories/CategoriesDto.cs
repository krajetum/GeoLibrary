namespace GeoLibrary.Server.Abstractions.Dtos.BookCategories;

public class CategoriesDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
}
