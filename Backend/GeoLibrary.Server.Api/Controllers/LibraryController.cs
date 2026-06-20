using AutoMapper;
using GeoLibrary.Server.Abstractions;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using GeoLibrary.Server.Abstractions.Validators;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LibraryController : ControllerBase
{

    private readonly GeoLibraryDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<LibraryController> _logger;
    private readonly IMapsService _mapsService;

    public LibraryController(GeoLibraryDbContext db, IMapper mapper,  ILogger<LibraryController> logger)
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
        {
            return BadRequest(validationResult.Errors);
        }

        var entity = _mapper.Map<AddLibraryDto, LibraryEntity>(libraryDto);

       


        await _db.Libraries.AddAsync(entity);
        await _db.SaveChangesAsync();

        return Ok(libraryDto);
    }




}
