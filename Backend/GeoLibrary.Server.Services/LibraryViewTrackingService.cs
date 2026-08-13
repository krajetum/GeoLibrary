using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Models;
using GeoLibrary.Server.Abstractions.Services;
using GeoLibrary.Server.Database;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Services;

public class LibraryViewTrackingService(IDistributedCache redis, GeoLibraryDbContext db) : ITrackingService<LibraryTrackingRequest>
{
    /// <summary>
    /// Registra una visualizzazione se il visitatore non l'ha già generata oggi.
    /// La finestra di deduplica scade a mezzanotte UTC, così da coincidere
    /// con il bucket giornaliero della tabella delle statistiche.
    /// </summary>
    public async Task<bool> TrackAsync(LibraryTrackingRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cacheKey = $"view:book:{request.LibraryId}:{today:yyyyMMdd}:{request.UserSignature}";

        if (redis.GetStringAsync(cacheKey) != null)
        {
            return false;
        }

        await redis.SetStringAsync(cacheKey, "1", new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        });

        return await UpdateViewCount(request.LibraryId, today);
    }

    /// <summary>
    /// Incrementa il contatore delle visualizzazioni giornaliere per il libro e la biblioteca specificati.
    /// </summary>
    /// <param name="bookId"></param>
    /// <param name="libraryId"></param>
    /// <param name="today"></param>
    /// <returns></returns>
    private async Task<bool> UpdateViewCount( Guid libraryId, DateOnly today)
    {
        if (await db.LibraryDailyViews.FindAsync(libraryId, today.ToDateTime(new TimeOnly(0, 0, 0))) is LibraryDailyViewEntity existing)
        {
            existing.ViewsCount++;
            await db.SaveChangesAsync();
            return true;
        }

        // Se non esiste ancora un record per oggi, creane uno nuovo
        await db.LibraryDailyViews.AddAsync(new LibraryDailyViewEntity
        {
            LibraryId = libraryId,
            Date = today.ToDateTime(new TimeOnly(0, 0, 0)),
            ViewsCount = 1,
        });

        return true;
    }

}