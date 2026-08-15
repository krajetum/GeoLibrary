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
        var cacheKey = $"view:library:{request.LibraryId}:{today:yyyyMMdd}:{request.UserSignature}";

        if (await redis.GetStringAsync(cacheKey) != null)
        {
            return false;
        }

        if (!await UpdateViewCount(request.LibraryId, today))
        {
            return false;
        }

        

        await redis.SetStringAsync(cacheKey, "1", new DistributedCacheEntryOptions()
        {
            AbsoluteExpiration = GetExpiration()
        });
        return true;
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
        // Creare DateTime con Kind=Utc per compatibilità con PostgreSQL timestamptz
        var utcMidnight = today.ToDateTime(new TimeOnly(0, 0, 0), DateTimeKind.Utc);

        if (await db.LibraryDailyViews.FindAsync(libraryId, utcMidnight) is LibraryDailyViewEntity existing)
        {
            existing.ViewsCount++;
            await db.SaveChangesAsync();
            return true;
        }

        // Se non esiste ancora un record per oggi, creane uno nuovo
        await db.LibraryDailyViews.AddAsync(new LibraryDailyViewEntity
        {
            LibraryId = libraryId,
            Date = utcMidnight,
            ViewsCount = 1,
        });
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Restituisce la data di scadenza della cache per le visualizzazioni giornaliere. 
    /// L'expiration è sempre a fine giornata UTC, così da coincidere con il bucket giornaliero della tabella delle statistiche.
    /// <returns></returns>
    public DateTime GetExpiration()
    {
        return DateTime.UtcNow.Date.AddDays(1);
    }

}