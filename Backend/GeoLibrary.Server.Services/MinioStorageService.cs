using GeoLibrary.Server.Abstractions.Services;
using Minio;
using Minio.DataModel.Args;
using SkiaSharp;

namespace GeoLibrary.Server.Services;

public class MinioStorageService(IMinioClient client) : IStorageService
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

    public async Task<string> UploadImage(Stream content, string fileName, string contentType)
    {
        var key = await Upload(content, fileName, contentType);

        // L'originale è già stato letto fino in fondo, si riparte dall'inizio per la miniatura.
        content.Position = 0;

        using var original = SKBitmap.Decode(content);

        // Formato non riconosciuto: si tiene l'originale e si rinuncia alla miniatura.
        if (original is null)
        {
            return key;
        }

        // Il lato lungo scende a 400px; il fattore non supera 1 per non ingrandire le immagini piccole.
        var scale = Math.Min(1f, Math.Min(
            (float)ThumbnailSize / original.Width,
            (float)ThumbnailSize / original.Height));

        var size = new SKImageInfo((int)(original.Width * scale), (int)(original.Height * scale));
        // Mitchell: ricampionamento cubico, senza scalettature nelle riduzioni forti.
        using var resized = original.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell));

        // Si tiene il formato di partenza: convertire un PNG con trasparenza in JPEG darebbe fondo nero.
        var format = contentType == "image/png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(format, 80);
        using var thumbnail = encoded.AsStream();

        await Put(thumbnail, ThumbnailKey(key), contentType);

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
}
