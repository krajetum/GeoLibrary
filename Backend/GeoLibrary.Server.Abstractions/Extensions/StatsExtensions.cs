using GeoLibrary.Server.Abstractions.Dtos.Library;

namespace GeoLibrary.Server.Abstractions.Extensions;

/// <summary>
/// Utility condivise dagli endpoint che restituiscono serie storiche giornaliere
/// (statistiche di libreria, di libro e pannello di amministrazione).
/// </summary>
public static class StatsExtensions
{
    /// <summary>
    /// Numero massimo di giorni richiedibili in una sola chiamata.
    /// </summary>
    public const int MaxRangeDays = 365;

    /// <summary>
    /// Normalizza e valida l'intervallo richiesto. Le ore vengono azzerate perché
    /// i bucket delle statistiche sono giornalieri (mezzanotte UTC).
    /// </summary>
    /// <returns>false se l'intervallo non è valido; in quel caso <paramref name="error"/> contiene il messaggio da restituire al client.</returns>
    public static bool TryNormalizeRange(DateTime from, DateTime to, out DateTime cleanedFrom, out DateTime cleanedTo, out string error)
    {
        // Le colonne delle statistiche sono timestamptz e i bucket sono scritti a
        // mezzanotte UTC: DateTime.Date azzera l'ora ma lascia Kind = Unspecified,
        // che Npgsql rifiuta di confrontare con una colonna "timestamp with time zone".
        cleanedFrom = ToUtcMidnight(from);
        cleanedTo = ToUtcMidnight(to);

        if (cleanedFrom > cleanedTo)
        {
            error = "La data di inizio non può essere successiva a quella di fine.";
            return false;
        }

        if ((cleanedTo - cleanedFrom).TotalDays > MaxRangeDays)
        {
            error = "Il range di date non può superare un anno.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Porta una data a mezzanotte UTC. Le date che arrivano dal client sono già
    /// espresse in UTC: se il Kind è locale si converte, altrimenti si dichiara.
    /// </summary>
    private static DateTime ToUtcMidnight(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }

    /// <summary>
    /// Riempie con ViewsCount = 0 i giorni dell'intervallo per cui non esiste
    /// alcuna riga di statistiche, così il grafico non ha buchi sull'asse dei tempi.
    /// </summary>
    public static List<DateStats> FillStats(DateTime cleanedFrom, DateTime cleanedTo, List<DateStats> stats)
    {
        var allDatesInRange = Enumerable.Range(0, (cleanedTo - cleanedFrom).Days + 1)
                              .Select(offset => cleanedFrom.AddDays(offset))
                              .ToList();

        var statsDict = stats.ToDictionary(s => s.Date, s => s.ViewsCount);

        var completeStats = allDatesInRange.Select(date => new DateStats
        {
            Date = DateOnly.FromDateTime(date),
            ViewsCount = statsDict.TryGetValue(DateOnly.FromDateTime(date), out var count) ? count : 0
        }).ToList();

        return completeStats;
    }
}
