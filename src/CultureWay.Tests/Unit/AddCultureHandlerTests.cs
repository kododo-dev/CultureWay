using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.API.AddCulture;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class AddCultureHandlerTests
{
    private static AddCultureHandler CreateHandler(
        IStore store,
        CultureWayOptions options,
        RequestLocalizationOptions? requestLocalizationOptions = null)
        => new(store, options, Options.Create(requestLocalizationOptions ?? new RequestLocalizationOptions()));

    [Fact]
    public async Task Handle_ValidCulture_AppendsToOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new AddCulture("fr"), CancellationToken.None);

        Assert.Contains("fr", options.SupportedCultures);
    }

    [Fact]
    public async Task Handle_ValidCulture_Succeeds()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        await handler.HandleAsync(new AddCulture("fr"), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ValidCulture_PersistsToStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new AddCulture("fr"), CancellationToken.None);

        await store.Received(1).AddSupportedCultureAsync("fr", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCulture_SyncedIntoRequestLocalizationOptions()
    {
        var requestLocalizationOptions = new RequestLocalizationOptions();
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options, requestLocalizationOptions);

        await handler.HandleAsync(new AddCulture("fr"), CancellationToken.None);

        Assert.Contains(requestLocalizationOptions.SupportedCultures!, c => c.Name == "fr");
        Assert.Contains(requestLocalizationOptions.SupportedUICultures!, c => c.Name == "fr");
    }

    [Fact]
    public async Task Handle_EmptyCulture_ThrowsBadRequest()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new AddCulture(""), CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, e.StatusCode);
        Assert.DoesNotContain("", options.SupportedCultures);
    }

    [Fact]
    public async Task Handle_InvalidCultureCode_ThrowsBadRequest()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new AddCulture("not-a-culture-!!"), CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, e.StatusCode);
        Assert.DoesNotContain("not-a-culture-!!", options.SupportedCultures);
    }

    [Fact]
    public async Task Handle_DuplicateCulture_ThrowsConflict()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new AddCulture("fr"), CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, e.StatusCode);
    }

    [Fact]
    public async Task Handle_DuplicateCulture_CaseInsensitive_ThrowsConflict()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var e = await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new AddCulture("FR"), CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, e.StatusCode);
    }

    [Fact]
    public async Task Handle_DuplicateCulture_DoesNotPersistToStore()
    {
        var store = Substitute.For<IStore>();
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(store, options);

        await Assert.ThrowsAsync<RequestException>(() => handler.HandleAsync(new AddCulture("fr"), CancellationToken.None));

        await store.DidNotReceive().AddSupportedCultureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
