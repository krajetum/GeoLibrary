using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.User;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Services;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Minio;
using System.Text.Json;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserController(GeoLibraryDbContext dbContext, IHttpContextAccessor contextAccessor, IDistributedCache cache, IStorageService storageClient, ILogger<UserController> logger, IMapper mapper) : ControllerBase
{
    /// <summary>Dimensione massima dell'avatar, allineata al form della libreria.</summary>
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedAvatarTypes = ["image/jpeg", "image/png"];

    private readonly GeoLibraryDbContext _dbContext = dbContext;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<UserController> _logger = logger;
    private readonly IStorageService _storageClient = storageClient;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Copia del profilo tenuta in cache. Contiene la chiave dell'avatar e non il
    /// suo URL: i link firmati scadono dopo un'ora, mentre questa voce non ha
    /// scadenza, quindi memorizzarli qui vorrebbe dire servire link morti.
    /// </summary>
    private sealed record CachedProfile(string DisplayName, string Email, string? AvatarKey);

    [HttpGet("profile/me")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if(!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return Unauthorized();
        }

        var cached = await _cache.GetStringAsync($"profile:{userId}", cancellationToken);

        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Found profile in cache for id: {}", userId);
            await _cache.RefreshAsync($"profile:{userId}", cancellationToken);

            var cachedProfile = JsonSerializer.Deserialize<CachedProfile>(cached);
            if (cachedProfile is not null)
            {
                return Ok(await BuildProfileAsync(cachedProfile));
            }
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if(user is null)
        {
            _logger.LogInformation("No user found in database for id {}, creating new", userId);
            if(!_contextAccessor.TryGetUsername(out var username) || !_contextAccessor.TryGetEmail(out var email))
            {
                _logger.LogInformation("Missing mandatory information in token: name and email");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var entity = new UserEntity()
            {
                Id = userId,
                Email = email,
                DisplayName = username
            };

            var entityAdded = await _dbContext.Users.AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Added entity user to DB");
            user = entityAdded.Entity;

        }

        await CacheProfileAsync(user, cancellationToken);

        return Ok(await BuildProfileAsync(user));
    }

    [HttpPatch("profile/me")]
    public async Task<IActionResult> UpdateProfile([FromBody] EditProfileDto profile, CancellationToken cancellationToken)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return Unauthorized();
        }

        var validator = new EditProfileDtoValidator();
        var validationResult = await validator.ValidateAsync(profile, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
           return BadRequest("The user cannot be updated");
        }
        user.DisplayName = profile.DisplayName;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await CacheProfileAsync(user, cancellationToken);

        return Ok(await BuildProfileAsync(user));
    }

    [HttpPatch("profile/me/avatar")]
    public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile avatar, CancellationToken cancellationToken)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return Unauthorized();
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, "The user cannot be updated");
        }

        if(avatar is null || avatar.Length == 0)
        {
            return BadRequest("No avatar file provided");
        }

        if (avatar.Length > MaxAvatarBytes)
        {
            return BadRequest("L'immagine non può superare i 5 MB.");
        }

        if (!AllowedAvatarTypes.Contains(avatar.ContentType))
        {
            return BadRequest("Sono ammessi solo file JPG o PNG.");
        }

        var previousKey = user.AvatarKey;

        using var stream = avatar.OpenReadStream();
        user.AvatarKey = await _storageClient.UploadImageAsync(stream, avatar.FileName, avatar.ContentType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await CacheProfileAsync(user, cancellationToken);

        // L'avatar precedente non serve più: si cancella dopo il salvataggio, così
        // un errore su MinIO non lascia il profilo che punta a un oggetto rimosso.
        if (!string.IsNullOrEmpty(previousKey) && !await _storageClient.DeleteImageAsync(previousKey))
        {
            _logger.LogWarning("Avatar precedente {Key} dell'utente {UserId} non rimosso da MinIO", previousKey, userId);
        }

        return Ok(await BuildProfileAsync(user));
    }

    /// <summary>
    /// Diritto all'oblio dell'utente. Elimina l'utente e tutte le sue librerie e libri.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("profile/me")]
    public async Task<IActionResult> DeleteProfile(CancellationToken cancellationToken)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var libraryKeys = await _dbContext.Libraries
            .Where(l => l.UserId == userId && l.ImageKey != null)
            .Select(l => l.ImageKey!)
            .ToListAsync(cancellationToken);

        var bookKeys = await _dbContext.Books
            .Where(b => b.Library.UserId == userId && b.CoverImageKey != "")
            .Select(b => b.CoverImageKey)
            .ToListAsync(cancellationToken);

        var avatarKey = user.AvatarKey;

        // Sfruttado il cascade delete, elimino prima le librerie e i libri associati all'utente, poi l'utente stesso.
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"profile:{userId}", cancellationToken);

        var keys = bookKeys.Concat(libraryKeys);
        if (!string.IsNullOrEmpty(avatarKey))
        {
            keys = keys.Append(avatarKey);
        }

        if (!await _storageClient.DeleteManyAsync(keys))
        {
            _logger.LogWarning("Utente {UserId} cancellato, ma alcuni oggetti restano su MinIO", userId);
        }

        // TODO: Cancellare anche da Keycloak
        return NoContent();
    }

    /// <summary>
    /// Compone il profilo da restituire al client, firmando al momento gli URL
    /// dell'avatar e della sua miniatura.
    /// </summary>
    private async Task<ProfileDto> BuildProfileAsync(UserEntity user)
    {
        var profile = _mapper.Map<ProfileDto>(user);
        await AttachAvatarUrlsAsync(profile, user.AvatarKey);
        return profile;
    }

    private async Task<ProfileDto> BuildProfileAsync(CachedProfile cached)
    {
        var profile = new ProfileDto
        {
            DisplayName = cached.DisplayName,
            Email = cached.Email,
        };
        await AttachAvatarUrlsAsync(profile, cached.AvatarKey);
        return profile;
    }

    private async Task AttachAvatarUrlsAsync(ProfileDto profile, string? avatarKey)
    {
        if (string.IsNullOrEmpty(avatarKey))
        {
            return;
        }

        profile.AvatarUrl = await _storageClient.GetUrl(avatarKey);
        profile.AvatarThumbnailUrl = await _storageClient.GetThumbnailUrl(avatarKey);
    }

    private Task CacheProfileAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var cached = new CachedProfile(user.DisplayName, user.Email, user.AvatarKey);
        return _cache.SetStringAsync($"profile:{user.Id}", JsonSerializer.Serialize(cached), cancellationToken);
    }
}
