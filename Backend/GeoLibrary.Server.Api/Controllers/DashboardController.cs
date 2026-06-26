using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GeoLibrary.Server.Abstractions.Extensions;
namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DashboardController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public IActionResult GetDashboardData()
    {
        // Retrieve the user ID from the claims
        if (!_httpContextAccessor.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var dashboardData = new
        {
            Message = $"Dashboard data for user {userId}",
            Timestamp = DateTime.UtcNow
        };
        return Ok(dashboardData);
    }


}
