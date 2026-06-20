using GeoLibrary.Server.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Database;

public class GeoLibraryDbContext(DbContextOptions<GeoLibraryDbContext> options) : DbContext(options)
{
    public DbSet<LibraryEntity> Libraries { get; set; }
    public DbSet<BooksEntity> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LibraryEntity>(entity =>
        {
            // Colonna PostGIS: Point con SRID 4326 (WGS84)
            entity.Property(l => l.Location)
                  .HasColumnType("geometry(Point, 4326)");

            // Indice GIST per query spaziali performanti
            entity.HasIndex(l => l.Location)
                  .HasMethod("gist");
        });
    }
}
