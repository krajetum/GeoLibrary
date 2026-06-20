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
        CreateMap<LibraryEntity, LibraryDto>();
        CreateMap<AddLibraryDto, LibraryEntity>();
    }

}
