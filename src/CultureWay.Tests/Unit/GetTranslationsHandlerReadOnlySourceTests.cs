using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.API.GetTranslations;
using Kododo.CultureWay.UI.DTO;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class GetTranslationsHandlerReadOnlySourceTests
{
    private static IStore StoreWith(params Translation[] translations)
    {
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(translations.ToList());
        return store;
    }

    private static IReadOnlySource SourceWith(params Translation[] translations)
    {
        var source = Substitute.For<IReadOnlySource>();
        source.GetDefaultsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Translation>>(translations.ToList()));
        return source;
    }

    private static Task<TranslationDto[]> Handle(IStore store, params IReadOnlySource[] sources)
        => new GetTranslationsHandler(store, sources).HandleAsync(new GetTranslations(), CancellationToken.None);

    [Fact]
    public async Task Handle_KeyOnlyInSource_ReturnedAsExternalDefault()
    {
        var result = await Handle(StoreWith(), SourceWith(new Translation("Title", "en", "Hello")));

        var dto = Assert.Single(result);
        Assert.Equal("Title", dto.Key);
        Assert.Equal("en", dto.Culture);
        Assert.Equal("Hello", dto.Value);
        Assert.True(dto.HasExternalDefault);
        Assert.Equal("Hello", dto.ExternalDefaultValue);
    }

    [Fact]
    public async Task Handle_KeyOnlyInStore_HasNoExternalDefault()
    {
        var result = await Handle(StoreWith(new Translation("Title", "en", "Stored")), SourceWith());

        var dto = Assert.Single(result);
        Assert.Equal("Stored", dto.Value);
        Assert.False(dto.HasExternalDefault);
        Assert.Null(dto.ExternalDefaultValue);
    }

    [Fact]
    public async Task Handle_StoreOverridesSource_ReturnsStoreValueAndKeepsExternalDefault()
    {
        var result = await Handle(
            StoreWith(new Translation("Title", "en", "Overridden")),
            SourceWith(new Translation("Title", "en", "Original")));

        var dto = Assert.Single(result);
        Assert.Equal("Overridden", dto.Value);
        Assert.True(dto.HasExternalDefault);
        Assert.Equal("Original", dto.ExternalDefaultValue);
    }

    [Fact]
    public async Task Handle_CultureComparisonIsCaseInsensitive()
    {
        var result = await Handle(
            StoreWith(new Translation("Title", "PL", "Nadpisane")),
            SourceWith(new Translation("Title", "pl", "Oryginal")));

        var dto = Assert.Single(result);
        Assert.Equal("Nadpisane", dto.Value);
        Assert.Equal("Oryginal", dto.ExternalDefaultValue);
    }

    [Fact]
    public async Task Handle_KeyComparisonIsCaseSensitive()
    {
        var result = await Handle(
            StoreWith(new Translation("title", "en", "Stored")),
            SourceWith(new Translation("Title", "en", "Original")));

        Assert.Equal(2, result.Count());
        Assert.False(result.Single(t => t.Key == "title").HasExternalDefault);
        Assert.True(result.Single(t => t.Key == "Title").HasExternalDefault);
    }

    [Fact]
    public async Task Handle_SameKeyInMultipleSources_FirstSourceWins()
    {
        var result = await Handle(
            StoreWith(),
            SourceWith(new Translation("Title", "en", "First")),
            SourceWith(new Translation("Title", "en", "Second")));

        Assert.Equal("First", Assert.Single(result).Value);
    }

    [Fact]
    public async Task Handle_MergesStoreAndSourceEntriesAcrossCultures()
    {
        var result = await Handle(
            StoreWith(new Translation("A", "en", "a-en-store")),
            SourceWith(
                new Translation("A", "en", "a-en-src"),
                new Translation("A", "pl", "a-pl-src"),
                new Translation("B", "en", "b-en-src")));

        Assert.Equal(3, result.Count());
        Assert.Equal("a-en-store", result.Single(t => t.Key == "A" && t.Culture == "en").Value);
        Assert.Equal("a-pl-src", result.Single(t => t.Key == "A" && t.Culture == "pl").Value);
        Assert.True(result.Single(t => t.Key == "B" && t.Culture == "en").HasExternalDefault);
    }

    [Fact]
    public async Task Handle_ResultIsOrderedByKeyThenCulture()
    {
        var result = await Handle(
            StoreWith(new Translation("b", "pl", "1"), new Translation("a", "pl", "2")),
            SourceWith(new Translation("b", "en", "3"), new Translation("a", "en", "4")));

        Assert.Equal(
            [("a", "en"), ("a", "pl"), ("b", "en"), ("b", "pl")],
            result.Select(t => (t.Key, t.Culture)));
    }
}
