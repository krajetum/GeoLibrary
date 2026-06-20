using GeoLibrary.Server.Abstractions.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Database;

public class GeoLibraryDbContext(DbContextOptions<GeoLibraryDbContext> options) : DbContext(options)
{

    public DbSet<LibraryEntity> Libraries { get; set;}
    public DbSet<BooksEntity> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
    }



}
