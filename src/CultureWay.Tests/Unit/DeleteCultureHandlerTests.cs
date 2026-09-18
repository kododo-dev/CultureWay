using FluentAssertions;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.CultureWay.UI.API.DeleteCulture;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class DeleteCultureHandlerTests
{
    private static DeleteCultureHandler CreateHandler(
        IStore store,
        CultureWayOptions options,
        RequestLocalizationOptions? requestLocalizationOptions = null)
        => new(store, new TranslationCache(), options, Options.Create(requestLocalizationOptions ?? new RequestLocalizationOptions()));

    [Fact]
    public async Task Handle_ExistingNonDefaultCulture_RemovesFromOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>());
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new DeleteCulture("fr"), CancellationToken.None);

        options.SupportedCultures.Should().NotContain("fr");
    }

    [Fact]
    public async Task Handle_ExistingNonDefaultCulture_ReturnsNoErrors()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>());
        var handler = CreateHandler(store, options);

        var result = await handler.HandleAsync(new DeleteCulture("fr"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExistingNonDefaultCulture_RemovedFromStore()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>());
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new DeleteCulture("fr"), CancellationToken.None);

        await store.Received(1).RemoveSupportedCultureAsync("fr", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingCulture_DeletesItsTranslations()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>
        {
            new("Hello", "en", "Hello"),
            new("Hello", "fr", "Bonjour"),
        });
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new DeleteCulture("fr"), CancellationToken.None);

        await store.Received(1).DeleteAsync(
            Arg.Is<IReadOnlyCollection<(string Key, string Culture)>>(k =>
                k.Count == 1 && k.Any(x => x.Key == "Hello" && x.Culture == "fr")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingCulture_SyncedOutOfRequestLocalizationOptions()
    {
        var requestLocalizationOptions = new RequestLocalizationOptions();
        requestLocalizationOptions.AddSupportedCultures("en", "fr");
        requestLocalizationOptions.AddSupportedUICultures("en", "fr");
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        store.GetAllAsync().Returns(new List<Translation>());
        var handler = CreateHandler(store, options, requestLocalizationOptions);

        await handler.HandleAsync(new DeleteCulture("fr"), CancellationToken.None);

        requestLocalizationOptions.SupportedCultures.Should().NotContain(c => c.Name == "fr");
        requestLocalizationOptions.SupportedUICultures.Should().NotContain(c => c.Name == "fr");
    }

    [Fact]
    public async Task Handle_EmptyCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new DeleteCulture(""), CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new DeleteCulture("de"), CancellationToken.None);

        result.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_DefaultCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new DeleteCulture("en"), CancellationToken.None);

        result.Should().ContainSingle();
        options.SupportedCultures.Should().Contain("en");
    }

    [Fact]
    public async Task Handle_LastRemainingCulture_ReturnsValidationError()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en"], DefaultCulture = "pl" };
        var handler = CreateHandler(Substitute.For<IStore>(), options);

        var result = await handler.HandleAsync(new DeleteCulture("en"), CancellationToken.None);

        result.Should().ContainSingle();
        options.SupportedCultures.Should().Contain("en");
    }

    [Fact]
    public async Task Handle_UnsupportedCulture_DoesNotTouchStore()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "fr"], DefaultCulture = "en" };
        var store = Substitute.For<IStore>();
        var handler = CreateHandler(store, options);

        await handler.HandleAsync(new DeleteCulture("de"), CancellationToken.None);

        await store.DidNotReceive().RemoveSupportedCultureAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
