using System.Globalization;
using FluentAssertions;
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

        localizer["Hello"].Value.Should().Be("Hello World");
    }

    [Fact]
    public void Indexer_ExactCultureMatch_ResourceNotFoundFalse()
    {
        var localizer = MakeLocalizer([new Translation("Hello", "en", "Hello World")], ["en"]);

        using var _ = SetUICulture("en");

        localizer["Hello"].ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_KeyNotFound_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        localizer["Missing"].ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public void Indexer_KeyNotFound_ValueIsKey()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        localizer["Missing.Key"].Value.Should().Be("Missing.Key");
    }

    [Fact]
    public void Indexer_CurrentCultureMissing_FallsBackToDefaultCulture()
    {
        var localizer = MakeLocalizer(
            [new Translation("Hello", "en", "Hello World")],
            ["en", "pl"], defaultCulture: "en");

        using var _ = SetUICulture("pl");

        var result = localizer["Hello"];
        result.Value.Should().Be("Hello World");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_BothCurrentAndDefaultMissing_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en", "pl"], defaultCulture: "en");

        using var _ = SetUICulture("pl");

        localizer["AnyKey"].ResourceNotFound.Should().BeTrue();
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

        localizer["Hello"].Value.Should().Be("American English");
    }

    [Fact]
    public void Indexer_RegionalCultureMissing_FallsBackToParentLanguage()
    {
        var localizer = MakeLocalizer(
            [new Translation("Hello", "en", "English")],
            ["en"]);

        using var _ = SetUICulture("en-GB");

        localizer["Hello"].Value.Should().Be("English");
    }

    [Fact]
    public void IndexerWithArgs_Found_FormatsValue()
    {
        var localizer = MakeLocalizer(
            [new Translation("Greeting", "en", "Hello, {0}!")],
            ["en"]);

        using var _ = SetUICulture("en");

        localizer["Greeting", "Alice"].Value.Should().Be("Hello, Alice!");
    }

    [Fact]
    public void IndexerWithArgs_NotFound_FormatsKey()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        localizer["Hello, {0}!", "World"].Value.Should().Be("Hello, World!");
    }

    [Fact]
    public void IndexerWithArgs_NotFound_ResourceNotFoundTrue()
    {
        var localizer = MakeLocalizer([], ["en"]);

        using var _ = SetUICulture("en");

        localizer["Key {0}", "x"].ResourceNotFound.Should().BeTrue();
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
        all.Should().HaveCount(2);
        all.Should().Contain(s => s.Name == "A" && s.Value == "en-A");
        all.Should().Contain(s => s.Name == "B" && s.Value == "en-B");
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

        all.Should().Contain(s => s.Name == "PlOnly");
        all.Should().Contain(s => s.Name == "EnOnly");
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
