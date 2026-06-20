using AutoMapper;
using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace GeoLibrary.Server.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LibraryController : ControllerBase
{
    private readonly GeoLibraryDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<LibraryController> _logger;

    private static readonly GeometryFactory _geometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public LibraryController(GeoLibraryDbContext db, IMapper mapper, ILogger<LibraryController> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> AddLibrary(AddLibraryDto libraryDto)
    {
        ArgumentNullException.ThrowIfNull(libraryDto);

        var validator = new AddLibraryDtoValidator();
        var validationResult = await validator.ValidateAsync(libraryDto);

        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        var entity = _mapper.Map<AddLibraryDto, LibraryEntity>(libraryDto);

        // NTS usa (X = Longitude, Y = Latitude)
        entity.Location = _geometryFactory.CreatePoint(
            new Coordinate(libraryDto.Longitude, libraryDto.Latitude));

        await _db.Libraries.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Ok(libraryDto);
    }

    /// <summary>
    /// Restituisce le librerie entro un raggio (in metri) da un punto geografico.
    /// Usa ST_DWithin con geography per distanza sferica accurata in metri.
    /// </summary>
    [HttpGet("search/radius")]
    public async Task<IActionResult> SearchByRadius([FromQuery] SearchByRadiusDto dto)
    {
        if (dto.RadiusKilometers <= 0)
            return BadRequest("RadiusKilometers deve essere maggiore di zero.");

        var center = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude, dto.Latitude));

        var libraries = await _db.Libraries
            .Where(l => l.Location != null &&
                        EF.Functions.IsWithinDistance(l.Location, center, dto.RadiusKilometers * 1000, true))
            .ToListAsync();

        return Ok(_mapper.Map<List<LibraryDto>>(libraries));
    }

    /// <summary>
    /// Restituisce le librerie che si trovano all'interno di un poligono disegnato sulla mappa.
    /// Le coordinate devono essere in ordine (senso orario o antiorario); il poligono viene chiuso automaticamente.
    /// </summary>
    [HttpPost("search/polygon")]
    public async Task<IActionResult> SearchByPolygon([FromBody] SearchByPolygonDto dto)
    {
        if (dto.Coordinates.Count < 3)
            return BadRequest("Servono almeno 3 coordinate per definire un poligono.");

        // Converte le coordinate in Coordinate NTS (X = Longitude, Y = Latitude)
        var coords = dto.Coordinates
            .Select(c => new Coordinate(c.Longitude, c.Latitude))
            .ToList();

        // Chiude l'anello ripetendo il primo punto alla fine
        coords.Add(coords[0]);

        var polygon = _geometryFactory.CreatePolygon([.. coords]);

        var libraries = await _db.Libraries
            .Where(l => l.Location != null && polygon.Contains(l.Location))
            .ToListAsync();

        return Ok(_mapper.Map<List<LibraryDto>>(libraries));
    }
}
