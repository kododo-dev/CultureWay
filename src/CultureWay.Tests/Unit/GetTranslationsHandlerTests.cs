using FluentAssertions;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.API.GetTranslations;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class GetTranslationsHandlerTests
{
    [Fact]
    public async Task Handle_EmptyStore_ReturnsEmptyArray()
    {
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>());
        var handler = new GetTranslationsHandler(store, []);

        var result = await handler.HandleAsync(new GetTranslations(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllTranslationsMappedToDto()
    {
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>
        {
            new("Hello", "en", "Hello World"),
            new("Hello", "pl", "Witaj Świecie"),
        });
        var handler = new GetTranslationsHandler(store, []);

        var result = await handler.HandleAsync(new GetTranslations(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello World");
        result.Should().Contain(t => t.Key == "Hello" && t.Culture == "pl" && t.Value == "Witaj Świecie");
    }

    [Fact]
    public async Task Handle_SortsByKeyThenCulture()
    {
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>
        {
            new("Zebra", "pl", "z-pl"),
            new("Apple", "en", "a-en"),
            new("Apple", "pl", "a-pl"),
            new("Banana", "en", "b-en"),
        });
        var handler = new GetTranslationsHandler(store, []);

        var result = await handler.HandleAsync(new GetTranslations(), CancellationToken.None);

        result.Select(t => (t.Key, t.Culture)).Should().Equal(
            ("Apple", "en"),
            ("Apple", "pl"),
            ("Banana", "en"),
            ("Zebra", "pl")
        );
    }

    [Fact]
    public async Task Handle_SortingIsCaseInsensitive()
    {
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>
        {
            new("apple", "en", "1"),
            new("Apple", "en", "2"),
        });
        var handler = new GetTranslationsHandler(store, []);

        var result = await handler.HandleAsync(new GetTranslations(), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
