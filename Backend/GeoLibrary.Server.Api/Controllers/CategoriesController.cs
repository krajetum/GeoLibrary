using GeoLibrary.Server.Abstractions.Dtos.BookCategories;
using GeoLibrary.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Api.Controllers;

/// <summary>
/// Elenco delle categorie dei libri. La lista è fissa (arriva dal seeding), quindi c'è solo la lettura.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly GeoLibraryDbContext _db;

    public CategoriesController(GeoLibraryDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Restituisce tutte le categorie, o solo quelle il cui nome contiene <paramref name="name"/>.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetCategories(string? name)
    {
        var query = _db.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(name))
        {
            // ILike e non Contains: la ricerca deve ignorare le maiuscole, come per i libri.
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{name.Trim()}%"));
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoriesDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug
            })
            .ToListAsync();

        return Ok(categories);
    }
}
