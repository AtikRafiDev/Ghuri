using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("Cox's Bazar", "cox-s-bazar")] // matches the blueprint's own example URL
    [InlineData("  Sylhet Tea Garden  ", "sylhet-tea-garden")]
    [InlineData("3 Days / 2 Nights!!", "3-days-2-nights")]
    [InlineData("Already-Slugged", "already-slugged")]
    public void GenerateFrom_ProducesLowercaseHyphenatedText(string input, string expected)
    {
        var result = Slug.GenerateFrom(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Slug.Create("   "));
    }
}
