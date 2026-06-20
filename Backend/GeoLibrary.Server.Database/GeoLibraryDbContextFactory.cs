using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GeoLibrary.Server.Database;

/// <summary>
/// Factory usata dagli strumenti EF Core (dotnet ef migrations) in design-time,
/// quando il contesto Aspire non è disponibile.
/// </summary>
public class GeoLibraryDbContextFactory : IDesignTimeDbContextFactory<GeoLibraryDbContext>
{
    public GeoLibraryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GeoLibraryDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=geolibrary;Username=postgres;Password=postgres",
                o => o.UseNetTopologySuite())
            .Options;

        return new GeoLibraryDbContext(options);
    }
}
