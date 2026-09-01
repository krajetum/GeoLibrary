namespace GeoLibrary.Server.Abstractions.Services;

public interface IStorageService
{
    /// <summary>
    /// Carica un file e restituisce la chiave con cui recuperarlo.
    /// </summary>
    Task<string> Upload(Stream content, string fileName, string contentType);

    /// <summary>
    /// Carica un'immagine insieme alla sua miniatura. Restituisce la chiave dell'originale.
    /// </summary>
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType);

    /// <summary>
    /// URL temporaneo per scaricare il file.
    /// </summary>
    Task<string> GetUrl(string key);

    /// <summary>
    /// URL temporaneo della miniatura generata da UploadImage.
    /// </summary>
    Task<string> GetThumbnailUrl(string key);

    /// <summary>
    /// Cancella un'immagine caricata con UploadImageAsync, miniatura compresa.
    /// Il prefisso della miniatura e' un dettaglio dell'implementazione, quindi
    /// la cancellazione della coppia sta qui e non nei chiamanti.
    /// </summary>
    Task<bool> DeleteImageAsync(string key);

    Task<bool> DeleteAsync(string key);
    Task<bool> DeleteManyAsync(IEnumerable<string> keys);
}
