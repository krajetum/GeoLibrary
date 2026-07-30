using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace GeoLibrary.Server.Services;

public class ISBNBookInfo
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string PublishedDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CoverUrl { get; set; } = string.Empty;
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

        return new ISBNBookInfo
        {
            Title = title ?? string.Empty,
            Author = author ?? string.Empty,
            Publisher = publisher ?? string.Empty,
            PublishedDate = publishedDate ?? string.Empty,
            Description = description ?? string.Empty,
            CoverUrl = coverUrl ?? string.Empty
        };
    }
}
