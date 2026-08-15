using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.User;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Abstractions.Services;
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
    private readonly GeoLibraryDbContext _dbContext = dbContext;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<UserController> _logger = logger;
    private readonly IStorageService _storageClient = storageClient;
    private readonly IMapper _mapper = mapper;

    [HttpGet("profile/me")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if(!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return Unauthorized();
        }

        var profile = await _cache.GetStringAsync($"profile:{userId}", cancellationToken);

        if (!string.IsNullOrEmpty(profile))
        {
            _logger.LogInformation("Found profile in cache for id: {}", userId);
            await _cache.RefreshAsync($"profile:{userId}");
            return Ok(JsonSerializer.Deserialize<ProfileDto>(profile));
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
        var userProfile = _mapper.Map<ProfileDto>(user);

        await _cache.SetStringAsync($"profile:{userId}", JsonSerializer.Serialize(userProfile), cancellationToken);


        return Ok(userProfile);
    }

    [HttpPatch("profile/me")]
    public async Task<IActionResult> UpdateProfile([FromBody] EditProfileDto profile, CancellationToken cancellationToken)
    {
        if (!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return Unauthorized();
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
           return BadRequest("The user cannot be updated");
        }
        user.DisplayName = profile.DisplayName;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.SetStringAsync($"profile:{userId}", JsonSerializer.Serialize(new ProfileDto
        {
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl
        }), cancellationToken);
        return Ok(profile);
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



        // Save the avatar to a file or a blob storage and get the URL
        using var stream = avatar.OpenReadStream();
        var avatarKey = await _storageClient.UploadImageAsync(stream, avatar.FileName, avatar.ContentType);

        user.AvatarUrl = await _storageClient.GetUrl(avatarKey);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.SetStringAsync($"profile:{userId}", JsonSerializer.Serialize(new ProfileDto
        {
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl
        }), cancellationToken);
        return Ok();
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

        // Sfruttado il cascade delete, elimino prima le librerie e i libri associati all'utente, poi l'utente stesso.
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync($"profile:{userId}", cancellationToken);

        var keys = bookKeys.Concat(libraryKeys);
        if (user.AvatarUrl is not null)
        {
            keys = keys.Append(user.AvatarUrl);
        }

        if (!await _storageClient.DeleteManyAsync(keys))
        {
            _logger.LogWarning("Utente {UserId} cancellato, ma alcuni oggetti restano su MinIO", userId);
        }
        
        // TODO: Cancellare anche da Keycloak
        return NoContent();
    }
}
