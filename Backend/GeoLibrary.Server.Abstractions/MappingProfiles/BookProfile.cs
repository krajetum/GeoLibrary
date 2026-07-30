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
        CreateMap<BookEntity, BookDto>()
            // Le URL firmate non stanno in tabella: le generano i controller
            .ForMember(d => d.CoverImageUrl, opt => opt.Ignore())
            .ForMember(d => d.CoverThumbnailUrl, opt => opt.Ignore())
            // IsAdmin lo valorizza il controller, come per LibraryDto
            .ForMember(d => d.IsAdmin, opt => opt.Ignore());
        CreateMap<AddBookDto, BookEntity>();
    }


}
