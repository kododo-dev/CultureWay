using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.API.SetDefaultCulture;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Http;
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

        Assert.Equal("fr", options.DefaultCulture);
    }

    [Fact]
    public async Task Handle_SupportedCulture_Succeeds()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new SetDefaultCulture("fr"), CancellationToken.None);
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

        Assert.Equal("fr", options.DefaultCulture);
    }

    [Fact]
    public async Task Handle_AlreadyDefaultCulture_Succeeds()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new SetDefaultCulture("en"), CancellationToken.None);
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
    public async Task Handle_EmptyCulture_ThrowsBadRequest()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new SetDefaultCulture(""), CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, e.StatusCode);
        Assert.Equal("en", options.DefaultCulture);
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_ThrowsNotFound()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new SetDefaultCulture("de"), CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, e.StatusCode);
        Assert.Equal("en", options.DefaultCulture);
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_DoesNotTouchStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new SetDefaultCulture("de"), CancellationToken.None));

        await store.DidNotReceive().SetDefaultCultureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
