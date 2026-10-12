using System.Globalization;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Localization;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class CultureWayStringLocalizerTests
{
    private static CultureWayStringLocalizer MakeLocalizer(
        IEnumerable<Translation> translations,
        string[] supportedCultures,
        string defaultCulture = "en")
    {
        var cache = new TranslationCache();
        cache.Load(translations);
        var options = new CultureWayOptions
        {
            SupportedCultures = supportedCultures,
            DefaultCulture     = defaultCulture,
        };
        return new CultureWayStringLocalizer(cache, options);
    }

    [Fact]
    public void Indexer_ExactCultureMatch_ReturnsValue()
    {
        var localizer = MakeLocalizer([new Translation("Hello", "en", "Hello World")], ["en"]);

        using var _ = SetUICulture("en");

        Assert.Equal("Hello World", localizer["Hello"].Value);
    }

    [Fact]
    public void Indexer_ExactCultureMatch_ResourceNotFoundFalse()
    {
        var localizer = MakeLocalizer([new Translation("Hello", "en", "Hello World")], ["en"]);

        using var _ = SetUICulture("en");

        Assert.False(localizer["Hello"].ResourceNotFound);
    }

    [Fact]
    public void Indexer_KeyNotFound_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        Assert.True(localizer["Missing"].ResourceNotFound);
    }

    [Fact]
    public void Indexer_KeyNotFound_ValueIsKey()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        Assert.Equal("Missing.Key", localizer["Missing.Key"].Value);
    }

    [Fact]
    public void Indexer_CurrentCultureMissing_FallsBackToDefaultCulture()
    {
        var localizer = MakeLocalizer(
            [new Translation("Hello", "en", "Hello World")],
            ["en", "pl"], defaultCulture: "en");

        using var _ = SetUICulture("pl");

        var result = localizer["Hello"];
        Assert.Equal("Hello World", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_BothCurrentAndDefaultMissing_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en", "pl"], defaultCulture: "en");

        using var _ = SetUICulture("pl");

        Assert.True(localizer["AnyKey"].ResourceNotFound);
    }

    [Fact]
    public void Indexer_SpecificRegionalCulture_UsesSpecificBeforeParent()
    {
        var localizer = MakeLocalizer(
            [
                new Translation("Hello", "en",    "English"),
                new Translation("Hello", "en-US", "American English"),
            ],
            ["en", "en-US"]);

        using var _ = SetUICulture("en-US");

        Assert.Equal("American English", localizer["Hello"].Value);
    }

    [Fact]
    public void Indexer_RegionalCultureMissing_FallsBackToParentLanguage()
    {
        var localizer = MakeLocalizer(
            [new Translation("Hello", "en", "English")],
            ["en"]);

        using var _ = SetUICulture("en-GB");

        Assert.Equal("English", localizer["Hello"].Value);
    }

    [Fact]
    public void IndexerWithArgs_Found_FormatsValue()
    {
        var localizer = MakeLocalizer(
            [new Translation("Greeting", "en", "Hello, {0}!")],
            ["en"]);

        using var _ = SetUICulture("en");

        Assert.Equal("Hello, Alice!", localizer["Greeting", "Alice"].Value);
    }

    [Fact]
    public void IndexerWithArgs_NotFound_FormatsKey()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        Assert.Equal("Hello, World!", localizer["Hello, {0}!", "World"].Value);
    }

    [Fact]
    public void IndexerWithArgs_NotFound_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        Assert.True(localizer["Key {0}", "x"].ResourceNotFound);
    }

    [Fact]
    public void GetAllStrings_IncludeParentFalse_ReturnsOnlyCurrentCulture()
    {
        var localizer = MakeLocalizer(
            [
                new Translation("A", "en", "en-A"),
                new Translation("B", "en", "en-B"),
                new Translation("A", "pl", "pl-A"),
            ],
            ["en", "pl"]);

        using var _ = SetUICulture("en");

        var all = localizer.GetAllStrings(includeParentCultures: false).ToList();
        Assert.Equal(2, all.Count());
        Assert.Contains(all, s => s.Name == "A" && s.Value == "en-A");
        Assert.Contains(all, s => s.Name == "B" && s.Value == "en-B");
    }

    [Fact]
    public void GetAllStrings_IncludeParentTrue_IncludesFallbacks()
    {
        var localizer = MakeLocalizer(
            [
                new Translation("EnOnly", "en", "en-only"),
                new Translation("PlOnly", "pl", "pl-only"),
            ],
            ["en", "pl"], defaultCulture: "en");

        using var _ = SetUICulture("pl");

        var all = localizer.GetAllStrings(includeParentCultures: true).ToList();

        Assert.Contains(all, s => s.Name == "PlOnly");
        Assert.Contains(all, s => s.Name == "EnOnly");
    }

    private static IDisposable SetUICulture(string culture)
    {
        var previous = Thread.CurrentThread.CurrentUICulture;
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        return new DelegateDisposable(() => Thread.CurrentThread.CurrentUICulture = previous);
    }

    private sealed class DelegateDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
