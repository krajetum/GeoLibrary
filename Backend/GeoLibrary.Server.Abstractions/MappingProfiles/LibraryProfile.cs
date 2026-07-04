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
            // BookCount e IsAdmin vengono valorizzati manualmente nei controller
            .ForMember(d => d.BookCount, opt => opt.Ignore())
            .ForMember(d => d.IsAdmin, opt => opt.Ignore());
        CreateMap<AddLibraryDto, LibraryEntity>();
    }

}
