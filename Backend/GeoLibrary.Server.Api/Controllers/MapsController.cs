using GeoLibrary.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoLibrary.Server.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MapsController : ControllerBase
{

    private readonly IMapsService _mapsService;
    private readonly ILogger<MapsController> _logger;

    public MapsController(IMapsService mapsService, ILogger<MapsController> logger)
    {
        ArgumentNullException.ThrowIfNull(mapsService, nameof(mapsService));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _mapsService = mapsService;
        _logger = logger;
    }

    public async Task<IActionResult> GetAddressInfo([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest("Address query parameter is required.");
        }
        try
        {
            var addressInfo = await _mapsService.GetAddressInfo(address);
            return Ok(addressInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching address information for address: {Address}", address);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }




}
