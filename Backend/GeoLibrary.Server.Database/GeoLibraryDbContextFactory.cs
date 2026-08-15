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
                "Host=localhost;Port=56189;Username=postgres;Password=G.Ag-F5u.)~spw{QwXn6PZ;Database=database",
                o => o.UseNetTopologySuite())
            .Options;

        return new GeoLibraryDbContext(options);
    }
}
