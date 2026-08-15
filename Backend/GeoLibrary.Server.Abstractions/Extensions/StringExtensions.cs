using System.Text;

namespace GeoLibrary.Server.Abstractions.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Riduce una stringa a lettere e numeri separati da trattini: "Fiction, fantasy" a "fiction-fantasy"
    /// Rimuove la punteggiatura
    /// </summary>
    public static string ToSlug(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        var slug = new StringBuilder();
        foreach (var c in str.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                slug.Append(c);
            }
            // Se piu caratteri non lettere o numeri consecutivi allora si converte con un singolo -
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        return slug.ToString().Trim('-');
    }
}
