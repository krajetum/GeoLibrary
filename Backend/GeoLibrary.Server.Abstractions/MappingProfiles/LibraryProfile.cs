using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.Library;
using GeoLibrary.Server.Abstractions.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.MappingProfiles;

public class LibraryProfile : Profile
{
    public LibraryProfile()
    {
        CreateMap<LibraryEntity, LibraryDto>()
            .ForMember(d => d.Latitude, opt => opt.MapFrom(s => s.Location.Y))
            .ForMember(d => d.Longitude, opt => opt.MapFrom(s => s.Location.X))
            // BookCount viene valorizzato manualmente nel controller (vedi GetLibraries)
            .ForMember(d => d.BookCount, opt => opt.Ignore());
        CreateMap<AddLibraryDto, LibraryEntity>();
    }

}
