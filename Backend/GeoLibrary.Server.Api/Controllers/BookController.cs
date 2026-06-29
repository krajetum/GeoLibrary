using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeoLibrary.Server.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class BookController : ControllerBase
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly GeoLibraryDbContext _db;
    private readonly ILogger<BookController> _logger;

    public BookController(IHttpContextAccessor contextAccessor, GeoLibraryDbContext db, ILogger<BookController> logger)
    {
        _contextAccessor = contextAccessor;
        _db = db;
        _logger = logger;
    }




}
