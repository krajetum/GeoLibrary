using GeoLibrary.Server.Abstractions.Extensions;

namespace GeoLibrary.Server.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Fiction, fantasy", "fiction-fantasy")]
    [InlineData("Storia", "storia")]
    [InlineData("  Saggistica  ", "saggistica")]
    [InlineData("Sci-Fi / Horror", "sci-fi-horror")]
    public void ToSlug_NormalizzaIlNome(string input, string atteso)
    {
        Assert.Equal(atteso, input.ToSlug());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void ToSlug_StringaSenzaCaratteriUtili_RestituisceVuoto(string input)
    {
        Assert.Equal(string.Empty, input.ToSlug());
    }
}
