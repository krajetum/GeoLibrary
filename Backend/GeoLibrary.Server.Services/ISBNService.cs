using GeoLibrary.Server.Abstractions.Extensions;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeoLibrary.Server.Services;

public class ISBNBookInfo
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
    public List<ISBNCategory> Categories { get; set; } = [];
}

public class ISBNCategory
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
}

public class ISBNService(HttpClient httpClient)
{
    public async Task<ISBNBookInfo?> FetchBookDetails(string isbn)
    {
        var bibKey = $"ISBN:{isbn}";
        var response = await httpClient.GetAsync($"?bibkeys={bibKey}&jscmd=data&format=json");
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (!doc.RootElement.TryGetProperty(bibKey, out var book))
            return null;

        var title = book.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;

        string? author = null;
        if (book.TryGetProperty("authors", out var authors) && authors.ValueKind == JsonValueKind.Array)
        {
            author = string.Join(", ", authors.EnumerateArray()
                .Select(x => x.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name)));
        }

        string? publisher = null;
        if (book.TryGetProperty("publishers", out var publishers) &&
            publishers.ValueKind == JsonValueKind.Array &&
            publishers.GetArrayLength() > 0)
        {
            publisher = publishers[0].TryGetProperty("name", out var publisherName) ? publisherName.GetString() : null;
        }

        var publishedDate = book.TryGetProperty("publish_date", out var date) ? date.GetString() : null;

        string? description = null;
        if (book.TryGetProperty("notes", out var notes))
        {
            description = notes.ValueKind == JsonValueKind.String
                ? notes.GetString()
                : notes.TryGetProperty("value", out var noteValue) ? noteValue.GetString() : null;
        }

        // La copertina arriva in tre misure e solo se esiste davvero: si prende la più grande.
        string? coverUrl = null;
        if (book.TryGetProperty("cover", out var cover))
        {
            coverUrl = cover.TryGetProperty("large", out var large) ? large.GetString()
                : cover.TryGetProperty("medium", out var medium) ? medium.GetString()
                : cover.TryGetProperty("small", out var small) ? small.GetString()
                : null;
        }

        var isbnCategories = new List<ISBNCategory>();
        if (book.TryGetProperty("subjects", out var subjects) && subjects.ValueKind == JsonValueKind.Array)
        {
            var categories = subjects.EnumerateArray()
                .Select(x => x.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            isbnCategories = [.. categories.Select(c => new ISBNCategory
            {
                Name = c!,
                Slug = c!.ToSlug()
            })];

        }

        return new ISBNBookInfo
        {
            Title = title ?? string.Empty,
            Author = author ?? string.Empty,
            Publisher = publisher ?? string.Empty,
            PublishedDate = ParsePublishedDate(publishedDate),
            Description = description ?? string.Empty,
            CoverUrl = coverUrl ?? string.Empty,
            Categories = isbnCategories
        };
    }

    /// <summary>
    /// OpenLibrary non ha un formato fisso per publish_date: "1998", "March 1998", "c1975", "1998-03-01".
    /// Si prova la data completa, poi si ripiega sul primo anno a quattro cifre; se non si capisce, null.
    /// </summary>
    private static DateTime? ParsePublishedDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.Date;

        var year = Regex.Match(value, @"\d{4}");
        if (year.Success && int.TryParse(year.Value, out var yearValue) && yearValue is >= 1 and <= 9999)
            return new DateTime(yearValue, 1, 1);

        return null;
    }
}
