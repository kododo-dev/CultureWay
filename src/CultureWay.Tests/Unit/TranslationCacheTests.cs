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

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_AfterLoad_ReturnsValue()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        var found = _cache.TryGetValue("Hello", "en", out var value);

        Assert.True(found);
        Assert.Equal("Hello World", value);
    }

    [Fact]
    public void TryGetValue_WrongCulture_ReturnsFalse()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        Assert.False(_cache.TryGetValue("Hello", "pl", out _));
    }

    [Fact]
    public void TryGetValue_WrongKey_ReturnsFalse()
    {
        _cache.Load([new Translation("Hello", "en", "Hello World")]);

        Assert.False(_cache.TryGetValue("Goodbye", "en", out _));
    }

    [Fact]
    public void Load_CalledTwice_UsesLatestSnapshot()
    {
        _cache.Load([new Translation("Key", "en", "FirstValue")]);
        _cache.Load([new Translation("Key", "en", "SecondValue")]);

        _cache.TryGetValue("Key", "en", out var value);

        Assert.Equal("SecondValue", value);
    }

    [Fact]
    public void Load_EmptyList_PreviousDataCleared()
    {
        _cache.Load([new Translation("Key", "en", "Value")]);
        _cache.Load([]);

        Assert.False(_cache.TryGetValue("Key", "en", out _));
    }

    [Fact]
    public void GetAll_AfterLoad_ReturnsAllTranslations()
    {
        _cache.Load([
            new Translation("A", "en", "en-A"),
            new Translation("A", "pl", "pl-A"),
        ]);

        Assert.Equal(2, _cache.GetAll().Count());
    }

    [Fact]
    public void GetAll_BeforeLoad_ReturnsEmpty()
    {
        Assert.Empty(_cache.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsCorrectKeysCulturesValues()
    {
        _cache.Load([new Translation("Title", "pl", "Tytuł")]);

        var result = _cache.GetAll().Single();

        Assert.Equal("Title", result.Key);
        Assert.Equal("pl", result.Culture);
        Assert.Equal("Tytuł", result.Value);
    }
}
