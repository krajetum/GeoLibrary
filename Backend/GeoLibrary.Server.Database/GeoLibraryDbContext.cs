using GeoLibrary.Server.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Database;

public class GeoLibraryDbContext(DbContextOptions<GeoLibraryDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<LibraryEntity> Libraries { get; set; }
    public DbSet<BookEntity> Books { get; set; }
    public DbSet<LoanRequestEntity> LoanRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LibraryEntity>(entity =>
        {
            entity.Property(l => l.Location)
                  .HasColumnType("geometry(Point, 4326)");

            entity.HasIndex(l => l.Location)
                  .HasMethod("gist");

            entity.HasOne<UserEntity>()
                  .WithMany(u => u.Libraries)
                  .HasForeignKey(l => l.UserId);
        });

        modelBuilder.Entity<LoanRequestEntity>(entity =>
        {
            entity.HasOne<UserEntity>()
                  .WithMany(u => u.Loans)
                  .HasForeignKey(lr => lr.UserId);

            entity.HasOne<BookEntity>()
                  .WithMany(b => b.Bookings)
                  .HasForeignKey(lr => lr.BookId);
        });

        modelBuilder.Entity<BookDailyViewEntity>(entity =>
        {
            entity.HasKey(bdv => new { bdv.BookId, bdv.Date });

            entity.HasOne(bdv => bdv.Book)
                  .WithMany(b => b.DailyViews)
                  .HasForeignKey(bdv => bdv.BookId);
        });

        modelBuilder.Entity<LibraryDailyViewEntity>(entity =>
        {
            entity.HasKey(ldv => new { ldv.LibraryId, ldv.Date });

            entity.HasOne(ldv => ldv.LibraryEntity)
                  .WithMany(l => l.DailyViews)
                  .HasForeignKey(ldv => ldv.LibraryId);
        });
    }
}
