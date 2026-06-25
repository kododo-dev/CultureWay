using FluentAssertions;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Localization;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class TranslationCacheTests
{
    private readonly TranslationCache _cache = new();

    [Fact]
    public void TryGetValue_BeforeLoad_ReturnsFalse()
    {
        var found = _cache.TryGetValue("Key", "en", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_AfterLoad_ReturnsValue()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        var found = _cache.TryGetValue("Hello", "en", out var value);

        found.Should().BeTrue();
        value.Should().Be("Hello World");
    }

    [Fact]
    public void TryGetValue_WrongCulture_ReturnsFalse()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        _cache.TryGetValue("Hello", "pl", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_WrongKey_ReturnsFalse()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        _cache.TryGetValue("Goodbye", "en", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_CalledTwice_UsesLatestSnapshot()
    {
        _cache.Load([new Translation("Key", "en", "FirstValue")]);
        _cache.Load([new Translation("Key", "en", "SecondValue")]);

        _cache.TryGetValue("Key", "en", out var value);

        value.Should().Be("SecondValue");
    }

    [Fact]
    public void Load_EmptyList_PreviousDataCleared()
    {
        _cache.Load([new Translation("Key", "en", "Value")]);
        _cache.Load([]);

        _cache.TryGetValue("Key", "en", out _).Should().BeFalse();
    }

    [Fact]
    public void GetAll_AfterLoad_ReturnsAllTranslations()
    {
        _cache.Load([
            new Translation("A", "en", "en-A"),
            new Translation("A", "pl", "pl-A"),
        ]);

        _cache.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void GetAll_BeforeLoad_ReturnsEmpty()
    {
        _cache.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetAll_ReturnsCorrectKeysCulturesValues()
    {
        _cache.Load([new Translation("Title", "pl", "Tytuł")]);

        var result = _cache.GetAll().Single();

        result.Key.Should().Be("Title");
        result.Culture.Should().Be("pl");
        result.Value.Should().Be("Tytuł");
    }
}
