using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.User;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Extensions;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UserController(GeoLibraryDbContext dbContext, IHttpContextAccessor contextAccessor, IDistributedCache cache, ILogger<UserController> logger, IMapper mapper) : ControllerBase
{
    private readonly GeoLibraryDbContext _dbContext = dbContext;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    private readonly IDistributedCache _cache = cache;
    private readonly ILogger<UserController> _logger = logger;
    private readonly IMapper _mapper = mapper;

    [HttpGet("profile/me")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        if(!_contextAccessor.TryGetUserId(out var userId))
        {
            _logger.LogError("The userId in the token is invalid or inexistent");
            return StatusCode(StatusCodes.Status500InternalServerError);
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
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Added entity user to DB");
            user = entityAdded.Entity;
            
        }
        var userProfile = _mapper.Map<ProfileDto>(user);

        await _cache.SetStringAsync($"profile:{userId}", JsonSerializer.Serialize(userProfile), cancellationToken);


        return Ok(userProfile);
    }

    

}
