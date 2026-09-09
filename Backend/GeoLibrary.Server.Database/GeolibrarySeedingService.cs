using GeoLibrary.Server.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Database;

public class GeolibrarySeedingService(GeoLibraryDbContext db)
{
    private readonly GeoLibraryDbContext _db = db;

    // AdminId: c5368d72-7a89-4ebb-b166-2e6c631e8342
    // TestUserId1: 3999c408-6f0b-492b-8a12-39bb2de80232
    // TestUserId2: 34f8f303-dc10-4fda-bf20-4831a146b97d
    public async Task SeedAsync()
    {

        var adminUser = new UserEntity
        {
            Id = Guid.Parse("c5368d72-7a89-4ebb-b166-2e6c631e8342"),
            DisplayName = "Admin",
            Email = "geolibrary.admin@gmail.com"
        };

        var testUser1 = new UserEntity
        {
            Id = Guid.Parse("3999c408-6f0b-492b-8a12-39bb2de80232"),
            DisplayName = "Test User 1",
            Email = "test.library1@gmail.com",

        };

        var testUser2 = new UserEntity
        {
            Id = Guid.Parse("34f8f303-dc10-4fda-bf20-4831a146b97d"),
            DisplayName = "Test User 2",
            Email = "test.library2@gmail.com"
        };

        await _db.Users.AddRangeAsync(adminUser, testUser1, testUser2);
            

        var library1 = new LibraryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Library 1",
            Location = new NetTopologySuite.Geometries.Point(12.4924, 41.8902) { SRID = 4326 },
            UserId = testUser1.Id,
            Address = "Piazza del Colosseo, 1, 00184 Roma RM, Italy",
            Country = "Italia",
            City = "Roma",
            CountryCode = "IT",
            PostalCode = "00184"
        };

        var library2 = new LibraryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Library 2",
            Location = new NetTopologySuite.Geometries.Point(12.4964, 41.9028) { SRID = 4326 },
            UserId = testUser2.Id,
            Address = "Piazza Venezia, 1, 00186 Roma RM, Italy",
            Country = "Italia",
            City = "Roma",
            CountryCode = "IT",
            PostalCode = "00186"
        };

        await _db.Libraries.AddRangeAsync(library1, library2);

        var categories = await _db.Categories.ToListAsync();

        var book1 = new BookEntity
        {
            Id = Guid.NewGuid(),
            ISBN = "9798686621558",
            Title = "Guerra Dei Mondi",
            Author = "H.G. Wells",
            TotalCopies = 1,
            Description = "La guerra dei mondi è un romanzo di fantascienza scritto da H. G. Wells.",
            LibraryId = library1.Id,
            Categories = new List<CategoryEntity>
            {
                categories.FirstOrDefault(c => c.Slug == "fiction")!,
                categories.FirstOrDefault(c => c.Slug == "science-fiction")!
            }
        };

        await _db.Books.AddAsync(book1);

        var book2 = new BookEntity
        {
            Id = Guid.NewGuid(),
            Title = "Eragon: Book I",
            Author = "Christopher Paolini",
            TotalCopies = 1,
            ISBN = "9788869180010",
            LibraryId = library2.Id,
            Categories = new List<CategoryEntity>
            {
                categories.FirstOrDefault(c => c.Slug == "fiction")!,
                categories.FirstOrDefault(c => c.Slug == "fantasy")!
            }
        };

        await _db.Books.AddAsync(book2);

        

        await _db.SaveChangesAsync();
    }
}
