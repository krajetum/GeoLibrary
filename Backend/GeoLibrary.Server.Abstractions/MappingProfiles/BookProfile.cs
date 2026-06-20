using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.MappingProfiles;

public class BookProfile : Profile
{
    public BookProfile()
    {
        CreateMap<BooksEntity, BookDto>();
    }


}
