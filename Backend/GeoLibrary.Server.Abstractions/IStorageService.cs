namespace GeoLibrary.Server.Abstractions;

public interface IStorageService
{
    /// <summary>
    /// Carica un file e restituisce la chiave con cui recuperarlo.
    /// </summary>
    Task<string> Upload(Stream content, string fileName, string contentType);

    /// <summary>
    /// Carica un'immagine insieme alla sua miniatura. Restituisce la chiave dell'originale.
    /// </summary>
    Task<string> UploadImage(Stream content, string fileName, string contentType);

    /// <summary>
    /// URL temporaneo per scaricare il file.
    /// </summary>
    Task<string> GetUrl(string key);

    /// <summary>
    /// URL temporaneo della miniatura generata da UploadImage.
    /// </summary>
    Task<string> GetThumbnailUrl(string key);
}
