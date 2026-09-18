using FluentAssertions;
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

        var dto = result.Should().ContainSingle().Subject;
        dto.Key.Should().Be("Title");
        dto.Culture.Should().Be("en");
        dto.Value.Should().Be("Hello");
        dto.HasExternalDefault.Should().BeTrue();
        dto.ExternalDefaultValue.Should().Be("Hello");
    }

    [Fact]
    public async Task Handle_KeyOnlyInStore_HasNoExternalDefault()
    {
        var result = await Handle(StoreWith(new Translation("Title", "en", "Stored")), SourceWith());

        var dto = result.Should().ContainSingle().Subject;
        dto.Value.Should().Be("Stored");
        dto.HasExternalDefault.Should().BeFalse();
        dto.ExternalDefaultValue.Should().BeNull();
    }

    [Fact]
    public async Task Handle_StoreOverridesSource_ReturnsStoreValueAndKeepsExternalDefault()
    {
        var result = await Handle(
            StoreWith(new Translation("Title", "en", "Overridden")),
            SourceWith(new Translation("Title", "en", "Original")));

        var dto = result.Should().ContainSingle().Subject;
        dto.Value.Should().Be("Overridden");
        dto.HasExternalDefault.Should().BeTrue();
        dto.ExternalDefaultValue.Should().Be("Original");
    }

    [Fact]
    public async Task Handle_CultureComparisonIsCaseInsensitive()
    {
        var result = await Handle(
            StoreWith(new Translation("Title", "PL", "Nadpisane")),
            SourceWith(new Translation("Title", "pl", "Oryginal")));

        var dto = result.Should().ContainSingle().Subject;
        dto.Value.Should().Be("Nadpisane");
        dto.ExternalDefaultValue.Should().Be("Oryginal");
    }

    [Fact]
    public async Task Handle_KeyComparisonIsCaseSensitive()
    {
        var result = await Handle(
            StoreWith(new Translation("title", "en", "Stored")),
            SourceWith(new Translation("Title", "en", "Original")));

        result.Should().HaveCount(2);
        result.Single(t => t.Key == "title").HasExternalDefault.Should().BeFalse();
        result.Single(t => t.Key == "Title").HasExternalDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SameKeyInMultipleSources_FirstSourceWins()
    {
        var result = await Handle(
            StoreWith(),
            SourceWith(new Translation("Title", "en", "First")),
            SourceWith(new Translation("Title", "en", "Second")));

        result.Should().ContainSingle().Which.Value.Should().Be("First");
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

        result.Should().HaveCount(3);
        result.Single(t => t.Key == "A" && t.Culture == "en").Value.Should().Be("a-en-store");
        result.Single(t => t.Key == "A" && t.Culture == "pl").Value.Should().Be("a-pl-src");
        result.Single(t => t.Key == "B" && t.Culture == "en").HasExternalDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ResultIsOrderedByKeyThenCulture()
    {
        var result = await Handle(
            StoreWith(new Translation("b", "pl", "1"), new Translation("a", "pl", "2")),
            SourceWith(new Translation("b", "en", "3"), new Translation("a", "en", "4")));

        result.Select(t => (t.Key, t.Culture)).Should().Equal(
            ("a", "en"), ("a", "pl"), ("b", "en"), ("b", "pl"));
    }
}
