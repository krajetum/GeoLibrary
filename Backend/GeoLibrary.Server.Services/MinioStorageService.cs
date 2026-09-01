using GeoLibrary.Server.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using SkiaSharp;

namespace GeoLibrary.Server.Services;

public class MinioStorageService(IMinioClient client, ILogger<MinioStorageService> logger) : IStorageService
{
    private const string BucketName = "geolibrary";
    private const int ThumbnailSize = 400;

    public async Task<string> Upload(Stream content, string fileName, string contentType)
    {
        // Chiave casuale: evita che due file con lo stesso nome si sovrascrivano.
        var key = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        await Put(content, key, contentType);

        return key;
    }

    public async Task<string> UploadImageAsync(Stream content, string fileName, string contentType)
    {
        // Tracce a livello Debug: un errore nel codice nativo di Skia non lascia
        // alcuna eccezione gestita, quindi in caso di problemi l'ultima riga
        // registrata è l'unico indizio su quale stadio si sia fermato.
        logger.LogDebug("Upload immagine {FileName} ({ContentType})", fileName, contentType);

        var key = await Upload(content, fileName, contentType);
        logger.LogDebug("Originale salvato con chiave {Key}", key);

        // L'originale è già stato letto fino in fondo, si riparte dall'inizio per la miniatura.
        content.Position = 0;

        using var original = SKBitmap.Decode(content);
        logger.LogDebug("Decodifica completata per {Key}", key);

        // Formato non riconosciuto: si tiene l'originale e si rinuncia alla miniatura.
        if (original is null)
        {
            return key;
        }

        // Il lato lungo scende a 400px; il fattore non supera 1 per non ingrandire le immagini piccole.
        var scale = Math.Min(1f, Math.Min(
            (float)ThumbnailSize / original.Width,
            (float)ThumbnailSize / original.Height));

        // Almeno un pixel per lato: con un'immagine molto allungata l'arrotondamento
        // porterebbe un lato a zero, e Skia ridimensiona verso una superficie vuota.
        var width = Math.Max(1, (int)(original.Width * scale));
        var height = Math.Max(1, (int)(original.Height * scale));
        var size = new SKImageInfo(width, height);

        // Mitchell: ricampionamento cubico, senza scalettature nelle riduzioni forti.
        logger.LogDebug("Ridimensionamento di {Key} a {Width}x{Height}", key, width, height);
        using var resized = original.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell));

        // Resize restituisce null se non riesce ad allocare o a convertire il formato:
        // passarlo a SKImage.FromBitmap farebbe cadere il processo nel codice nativo.
        if (resized is null)
        {
            logger.LogWarning("Ridimensionamento non riuscito per {Key}: miniatura non generata", key);
            return key;
        }

        // Si tiene il formato di partenza: convertire un PNG con trasparenza in JPEG darebbe fondo nero.
        var format = contentType == "image/png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        using var image = SKImage.FromBitmap(resized);
        if (image is null)
        {
            logger.LogWarning("Impossibile creare l'immagine per {Key}: miniatura non generata", key);
            return key;
        }

        using var encoded = image.Encode(format, 80);
        if (encoded is null)
        {
            logger.LogWarning("Codifica della miniatura non riuscita per {Key}", key);
            return key;
        }

        using var thumbnail = encoded.AsStream();
        logger.LogDebug("Miniatura codificata per {Key}, salvataggio in corso", key);

        await Put(thumbnail, ThumbnailKey(key), contentType);
        logger.LogDebug("Miniatura salvata per {Key}", key);

        return key;
    }

    public Task<string> GetUrl(string key)
    {
        // Il bucket non è pubblico, quindi si passa da un link firmato valido un'ora.
        return client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(key)
            .WithExpiry(3600));
    }

    public Task<string> GetThumbnailUrl(string key)
    {
        return GetUrl(ThumbnailKey(key));
    }

    private static string ThumbnailKey(string key)
    {
        return $"thumb-{key}";
    }

    private async Task Put(Stream content, string key, string contentType)
    {
        await EnsureBucketExists();
        logger.LogDebug("PutObject {Key} ({Size} byte)", key, content.Length);

        await client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType));
    }

    private async Task EnsureBucketExists()
    {
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
        }
    }

    public async Task<bool> DeleteImageAsync(string key)
    {
        var original = await DeleteAsync(key);
        var thumbnail = await DeleteAsync(ThumbnailKey(key));
        return original && thumbnail;
    }

    public Task<bool> DeleteAsync(string key)
    {
        return client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(key))
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.LogError(t.Exception, "Error occurred while deleting object from storage.");
                    return false;
                }
                return true;
            });
    }

    public Task<bool> DeleteManyAsync(IEnumerable<string> keys)
    {
        var tasks = keys.Select(DeleteAsync);
        return Task.WhenAll(tasks).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                logger.LogError(t.Exception, "Error occurred while deleting multiple objects from storage.");
                return false;
            }
            return t.Result.All(result => result);
        });
    }
}
