using GeoLibrary.Server.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;

namespace GeoLibrary.Server.Database;

public class GeoLibraryDbContext(DbContextOptions<GeoLibraryDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<LibraryEntity> Libraries { get; set; }
    public DbSet<BookEntity> Books { get; set; }
    public DbSet<LoanRequestEntity> LoanRequests { get; set; }
    public DbSet<BookDailyViewEntity> BookDailyViews { get; set; }
    public DbSet<LibraryDailyViewEntity> LibraryDailyViews { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
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
            entity.HasKey(bdv => new { bdv.BookId, bdv.LibraryId, bdv.Date });

            entity.HasOne(bdv => bdv.Book)
                  .WithMany(b => b.DailyViews)
                  .HasForeignKey(bdv => bdv.BookId);

            entity.HasOne(bdv => bdv.Library)
                  .WithMany()
                  .HasForeignKey(bdv => bdv.LibraryId);
        });

        modelBuilder.Entity<LibraryDailyViewEntity>(entity =>
        {
            entity.HasKey(ldv => new { ldv.LibraryId, ldv.Date });

            entity.HasOne(ldv => ldv.LibraryEntity)
                  .WithMany(l => l.DailyViews)
                  .HasForeignKey(ldv => ldv.LibraryId);
        });

        modelBuilder.Entity<CategoryEntity>(e =>
        {
            e.ToTable("Categories");

            e.HasIndex(c => c.Slug).IsUnique();

            // Seeding dei dati iniziali per le categorie di libri.
            // I Guid sono fissi e non generati: con Guid.NewGuid() ogni "migrations add"
            // vedrebbe valori diversi e genererebbe una delete+insert delle 14 righe.
            e.HasData(
                new { Id = new Guid("9b184f35-d69e-49ff-ada5-49d7f5f0b231"), Slug = "fiction", Name = "Narrativa" },
                new { Id = new Guid("2bd7d3fa-d983-4c7f-874c-459f6ba3988f"), Slug = "science-fiction", Name = "Fantascienza" },
                new { Id = new Guid("4c86ceb7-77b3-4450-81e5-a6ca7b297721"), Slug = "fantasy", Name = "Fantasy" },
                new { Id = new Guid("fc74ad2c-70a7-4ac0-bf39-70b545b9e8ba"), Slug = "mystery", Name = "Giallo" },
                new { Id = new Guid("f6a3b357-9644-4b57-9c9f-54feca152ac7"), Slug = "biography", Name = "Biografia" },
                new { Id = new Guid("f02b09b5-2e5b-4e65-b5e7-d64ebc8ba119"), Slug = "history", Name = "Storia" },
                new { Id = new Guid("b9728b09-5beb-4a2a-bbe3-8307a5ebeec3"), Slug = "science", Name = "Scienza" },
                new { Id = new Guid("18e3f450-e6d9-426a-a346-9be2544624f4"), Slug = "art", Name = "Arte" },
                new { Id = new Guid("4549113b-348b-4a70-8e77-997ad5236ba4"), Slug = "poetry", Name = "Poesia" },
                new { Id = new Guid("e31badcc-6021-4f60-b768-13aedad23e60"), Slug = "philosophy", Name = "Filosofia" },
                new { Id = new Guid("b92335fe-c8ef-46e6-8146-ce1741f68b71"), Slug = "romance", Name = "Romanzi" },
                new { Id = new Guid("86194ae9-2f73-4d8b-83f4-c58fb4c9f322"), Slug = "thriller", Name = "Thriller" },
                new { Id = new Guid("9384bd91-3151-41c9-aa11-2bfdbd15f83a"), Slug = "law", Name = "Diritto" },
                new { Id = new Guid("85e7682b-d147-495c-b1b0-3f550eb896a1"), Slug = "technology", Name = "Tecnologia" }
            );
        });

        modelBuilder.Entity<BookEntity>(entity =>
        {
            // Data di pubblicazione, non un istante: "date" evita il problema di Npgsql
            // con i DateTime di Kind Unspecified su colonne timestamp with time zone.
            entity.Property(b => b.PublishedAt)
                  .HasColumnType("date");

            entity.HasMany(b => b.Categories)
                  .WithMany(c => c.Books)
                  .UsingEntity<BookCategoryEntity>(
                      j => j.HasOne<CategoryEntity>().WithMany().HasForeignKey(e => e.CategoryId),
                      j => j.HasOne<BookEntity>().WithMany().HasForeignKey(e => e.BookId),
                      j => j.ToTable("BookCategories"));
        });

    }
}
