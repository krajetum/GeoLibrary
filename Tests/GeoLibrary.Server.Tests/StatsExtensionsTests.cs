using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Extensions;

namespace GeoLibrary.Server.Tests;

public class StatsExtensionsTests
{
    [Fact]
    public void TryNormalizeRange_AzzeraLOraEImpostaKindUtc()
    {
        var from = new DateTime(2025, 3, 10, 14, 30, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 3, 12, 8, 0, 0, DateTimeKind.Utc);

        var ok = StatsExtensions.TryNormalizeRange(from, to, out var cleanedFrom, out var cleanedTo, out var error);

        Assert.True(ok);
        Assert.Equal(string.Empty, error);
        Assert.Equal(new DateTime(2025, 3, 10), cleanedFrom);
        Assert.Equal(new DateTime(2025, 3, 12), cleanedTo);
        Assert.Equal(DateTimeKind.Utc, cleanedFrom.Kind);
        Assert.Equal(DateTimeKind.Utc, cleanedTo.Kind);
    }

    [Fact]
    public void TryNormalizeRange_InizioDopoLaFine_Fallisce()
    {
        var from = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var ok = StatsExtensions.TryNormalizeRange(from, to, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void TryNormalizeRange_IntervalloOltreUnAnno_Fallisce()
    {
        var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(StatsExtensions.MaxRangeDays + 1);

        var ok = StatsExtensions.TryNormalizeRange(from, to, out _, out _, out var error);

        Assert.False(ok);
        Assert.NotEmpty(error);
    }

    [Fact]
    public void FillStats_AggiungeIGiorniMancantiAZero()
    {
        var from = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 4, 0, 0, 0, DateTimeKind.Utc);
        var stats = new List<DateStats>
        {
            new() { Date = new DateOnly(2025, 6, 2), ViewsCount = 7 }
        };

        var risultato = StatsExtensions.FillStats(from, to, stats);

        Assert.Equal(4, risultato.Count);
        Assert.Equal([0L, 7L, 0L, 0L], risultato.Select(s => s.ViewsCount));
        Assert.Equal(new DateOnly(2025, 6, 1), risultato[0].Date);
        Assert.Equal(new DateOnly(2025, 6, 4), risultato[^1].Date);
    }
}
