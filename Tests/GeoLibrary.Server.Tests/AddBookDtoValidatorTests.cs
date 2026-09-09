using GeoLibrary.Server.Abstractions.Dtos.Book;
using GeoLibrary.Server.Abstractions.Validators;

namespace GeoLibrary.Server.Tests;

public class AddBookDtoValidatorTests
{
    private readonly AddBookDtoValidator _validator = new();

    private static AddBookDto Valido()
    {
        return new() {
            LibraryId = Guid.NewGuid(),
            Title = "Il nome della rosa",
            Author = "Umberto Eco",
            TotalCopies = 3
        };
    }

    [Fact]
    public void LibroCompleto_Valido()
    {
        Assert.True(_validator.Validate(Valido()).IsValid);
    }

    [Fact]
    public void TitoloVuoto_NonValido()
    {
        var dto = Valido();
        dto.Title = string.Empty;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void ZeroCopie_NonValido()
    {
        var dto = Valido();
        dto.TotalCopies = 0;

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void PiuDiCinqueCategorie_NonValido()
    {
        var dto = Valido();
        dto.Categories = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();

        Assert.False(_validator.Validate(dto).IsValid);
    }

    [Fact]
    public void DataDiPubblicazioneNelFuturo_NonValido()
    {
        var dto = Valido();
        dto.PublishedAt = DateTime.UtcNow.AddDays(1);

        Assert.False(_validator.Validate(dto).IsValid);
    }
}
