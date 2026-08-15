using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Dtos.BookCategories;
using GeoLibrary.Server.Abstractions.Entities;

namespace GeoLibrary.Server.Abstractions.MappingProfiles;

public class BookProfile : Profile
{
    public BookProfile()
    {
        // Serve anche a ProjectTo, che così proietta le categorie annidate in BookDto
        CreateMap<CategoryEntity, CategoriesDto>();

        CreateMap<BookEntity, BookDto>()
            // Le URL firmate non stanno in tabella: le generano i controller
            .ForMember(d => d.CoverImageUrl, opt => opt.Ignore())
            .ForMember(d => d.CoverThumbnailUrl, opt => opt.Ignore())
            // IsAdmin lo valorizza il controller, come per LibraryDto
            .ForMember(d => d.IsAdmin, opt => opt.Ignore());

        CreateMap<AddBookDto, BookEntity>()
            // Stesso nome ma tipi diversi (List<Guid> vs ICollection<CategoryEntity>):
            // le categorie le risolve il controller leggendole dal database.
            .ForMember(d => d.Categories, opt => opt.Ignore());
    }
}
