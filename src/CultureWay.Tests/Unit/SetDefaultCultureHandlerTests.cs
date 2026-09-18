using FluentAssertions;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.API.SetDefaultCulture;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class SetDefaultCultureHandlerTests
{
    private static SetDefaultCultureHandler CreateHandler(IStore store, CultureWayOptions options)
        => new(store, options);

    [Fact]
    public async Task Handle_SupportedCulture_UpdatesOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new SetDefaultCulture("fr"), CancellationToken.None);

        options.DefaultCulture.Should().Be("fr");
    }

    [Fact]
    public async Task Handle_SupportedCulture_ReturnsNoErrors()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new SetDefaultCulture("fr"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SupportedCulture_PersistsToStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new SetDefaultCulture("fr"), CancellationToken.None);

        await store.Received(1).SetDefaultCultureAsync("fr", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CaseInsensitiveMatch_UpdatesOptionsWithConfiguredCasing()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new SetDefaultCulture("FR"), CancellationToken.None);

        options.DefaultCulture.Should().Be("fr");
    }

    [Fact]
    public async Task Handle_AlreadyDefaultCulture_ReturnsNoErrors()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new SetDefaultCulture("en"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AlreadyDefaultCulture_DoesNotTouchStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new SetDefaultCulture("en"), CancellationToken.None);

        await store.DidNotReceive().SetDefaultCultureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new SetDefaultCulture(""), CancellationToken.None);

        result.Should().ContainSingle();
        options.DefaultCulture.Should().Be("en");
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new SetDefaultCulture("de"), CancellationToken.None);

        result.Should().ContainSingle();
        options.DefaultCulture.Should().Be("en");
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_DoesNotTouchStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new SetDefaultCulture("de"), CancellationToken.None);

        await store.DidNotReceive().SetDefaultCultureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
