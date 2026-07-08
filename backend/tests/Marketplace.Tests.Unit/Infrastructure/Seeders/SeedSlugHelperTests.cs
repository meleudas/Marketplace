using System.Text.RegularExpressions;
using Marketplace.Infrastructure.Persistence.Seeders;

namespace Marketplace.Tests.Unit.Infrastructure.Seeders;

public sealed class SeedSlugHelperTests
{
    private static readonly Regex LatinSlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    [Theory]
    [InlineData("Солодка Даруся", "solodka-darusia")]
    [InlineData("1984", "1984")]
    [InlineData("Clean Code", "clean-code")]
    [InlineData("Docker: Up & Running", "docker-up-running")]
    public void ToSlug_TransliteratesUkrainianTitles(string input, string expected)
    {
        var slug = SeedSlugHelper.ToSlug(input);

        Assert.Equal(expected, slug);
        Assert.Matches(LatinSlugRegex, slug);
    }

    [Fact]
    public void ToSlug_DoesNotContainCyrillic()
    {
        var slug = SeedSlugHelper.ToSlug("Гаррі Поттер і філософський камінь");

        Assert.DoesNotMatch(@"[\u0400-\u04FF]", slug);
        Assert.Equal("harri-potter-i-filosofskyi-kamin", slug);
    }
}
