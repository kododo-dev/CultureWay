using System.Globalization;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class ScopedLocalizerTests
{
    private sealed class CheckoutPage;

    [LocalizationScope("Shop.Checkout")]
    private sealed class ScopedByAttribute;

    [LocalizationScope("")]
    private sealed class SharedResources;

    private sealed class Generic<T>;

    private static (CultureWayStringLocalizerFactory Factory, CultureWayOptions Options) MakeFactory(
        IEnumerable<Translation> translations, string defaultCulture = "en", Action<CultureWayOptions>? configure = null)
    {
        var cache = new TranslationCache();
        cache.Load(translations);
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl"], DefaultCulture = defaultCulture };
        configure?.Invoke(options);
        return (new CultureWayStringLocalizerFactory(cache, options), options);
    }

    // ── Scope of a type ──────────────────────────────────────────────────────

    [Fact]
    public void DefaultScopeOf_IsTheShortTypeName()
        => Assert.Equal("CheckoutPage", CultureWayOptions.DefaultScopeOf(typeof(CheckoutPage)));

    [Fact]
    public void DefaultScopeOf_DropsTheGenericArity()
        => Assert.Equal("Generic", CultureWayOptions.DefaultScopeOf(typeof(Generic<int>)));

    [Fact]
    public void DefaultScopeOf_UsesTheAttribute()
        => Assert.Equal("Shop.Checkout", CultureWayOptions.DefaultScopeOf(typeof(ScopedByAttribute)));

    [Fact]
    public void DefaultScopeOf_EmptyAttribute_MeansUnscoped()
        => Assert.Equal("", CultureWayOptions.DefaultScopeOf(typeof(SharedResources)));

    // ── Lookup ───────────────────────────────────────────────────────────────

    [Fact]
    public void Lookup_ReadsTheScopedKey()
    {
        var (factory, _) = MakeFactory([new Translation("CheckoutPage.Pay", "en", "Pay now")]);
        using var _ = SetUICulture("en");

        var result = factory.Create(typeof(CheckoutPage))["Pay"];

        Assert.Equal("Pay now", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Lookup_PrefersTheScopedKeyOverTheUnscopedOne()
    {
        var (factory, _) = MakeFactory(
        [
            new Translation("Pay", "en", "Pay"),
            new Translation("CheckoutPage.Pay", "en", "Pay now"),
        ]);
        using var _ = SetUICulture("en");

        Assert.Equal("Pay now", factory.Create(typeof(CheckoutPage))["Pay"].Value);
    }

    [Fact]
    public void Lookup_ScopedKeyMissing_FallsBackToTheUnscopedKey()
    {
        var (factory, _) = MakeFactory([new Translation("Cancel", "en", "Cancel")]);
        using var _ = SetUICulture("en");

        var result = factory.Create(typeof(CheckoutPage))["Cancel"];

        Assert.Equal("Cancel", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Lookup_TriesTheUsersLanguageBeforeTheScope()
    {
        // An unscoped key in the user's language beats a scoped key in the default culture.
        var (factory, _) = MakeFactory(
        [
            new Translation("CheckoutPage.Pay", "en", "Pay now"),
            new Translation("Pay", "pl", "Zapłać"),
        ]);
        using var _ = SetUICulture("pl");

        Assert.Equal("Zapłać", factory.Create(typeof(CheckoutPage))["Pay"].Value);
    }

    [Fact]
    public void Lookup_WithoutFallback_IgnoresTheUnscopedKey()
    {
        var (factory, _) = MakeFactory(
            [new Translation("Cancel", "en", "Cancel")],
            configure: o => o.FallBackToUnscopedKeys = false);
        using var _ = SetUICulture("en");

        var result = factory.Create(typeof(CheckoutPage))["Cancel"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("Cancel", result.Value);
    }

    [Fact]
    public void Lookup_NotFound_ReportsTheScopedKeyAsSearchedLocation()
    {
        var (factory, _) = MakeFactory([]);
        using var _ = SetUICulture("en");

        var result = factory.Create(typeof(CheckoutPage))["Pay"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("Pay", result.Value);
        Assert.Equal("CheckoutPage.Pay", result.SearchedLocation);
    }

    [Fact]
    public void Lookup_WithArguments_FormatsTheScopedValue()
    {
        var (factory, _) = MakeFactory([new Translation("CheckoutPage.Total", "en", "Total: {0}")]);
        using var _ = SetUICulture("en");

        Assert.Equal("Total: 42", factory.Create(typeof(CheckoutPage))["Total", 42].Value);
    }

    [Fact]
    public void Lookup_UnscopedType_ReadsTheKeyAsIs()
    {
        var (factory, _) = MakeFactory([new Translation("Pay", "en", "Pay")]);
        using var _ = SetUICulture("en");

        Assert.Equal("Pay", factory.Create(typeof(SharedResources))["Pay"].Value);
        Assert.False(factory.Create(typeof(SharedResources))["Pay"].ResourceNotFound);
    }

    [Fact]
    public void Lookup_UsesTheConfiguredScopeOf()
    {
        var (factory, _) = MakeFactory(
            [new Translation("Custom.Pay", "en", "Pay now")],
            configure: o => o.ScopeOf = _ => "Custom");
        using var _ = SetUICulture("en");

        Assert.Equal("Pay now", factory.Create(typeof(CheckoutPage))["Pay"].Value);
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SameScope_ReturnsTheSameLocalizer()
    {
        var (factory, _) = MakeFactory([]);

        Assert.Same(factory.Create(typeof(CheckoutPage)), factory.Create(typeof(CheckoutPage)));
        Assert.NotSame(factory.Create(typeof(CheckoutPage)), factory.Create(typeof(SharedResources)));
    }

    [Fact]
    public void Create_ByBaseName_DropsTheAssemblyPrefix()
    {
        var (factory, _) = MakeFactory([new Translation("Views.Home.Index.Title", "en", "Home")]);
        using var _ = SetUICulture("en");

        var localizer = factory.Create("MyApp.Views.Home.Index", "MyApp");

        Assert.Equal("Home", localizer["Title"].Value);
    }

    // ── GetAllStrings ────────────────────────────────────────────────────────

    [Fact]
    public void GetAllStrings_ReturnsTheScopesKeysWithoutThePrefix()
    {
        var (factory, _) = MakeFactory(
        [
            new Translation("CheckoutPage.Pay", "en", "Pay now"),
            new Translation("CheckoutPage.Cancel", "en", "Cancel order"),
            new Translation("CheckoutPageExtra.Other", "en", "Not in scope"),
            new Translation("Pay", "en", "Pay"),
        ]);
        using var _ = SetUICulture("en");

        var all = factory.Create(typeof(CheckoutPage)).GetAllStrings(includeParentCultures: true)
            .ToDictionary(s => s.Name, s => s.Value);

        Assert.Equal(new Dictionary<string, string> { ["Pay"] = "Pay now", ["Cancel"] = "Cancel order" }, all);
    }

    // ── Registration ─────────────────────────────────────────────────────────

    [Fact]
    public void TypedLocalizer_FromTheContainer_UsesTheTypesScope()
    {
        var services = new ServiceCollection();
        services.AddCultureWay(x => x.Options.SupportedCultures = ["en"]);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<TranslationCache>().Load(
        [
            new Translation("Pay", "en", "Pay"),
            new Translation("CheckoutPage.Pay", "en", "Pay now"),
            new Translation("Shop.Checkout.Pay", "en", "Pay by attribute"),
        ]);
        using var _ = SetUICulture("en");

        Assert.Equal("Pay now", provider.GetRequiredService<IStringLocalizer<CheckoutPage>>()["Pay"].Value);
        Assert.Equal("Pay by attribute", provider.GetRequiredService<IStringLocalizer<ScopedByAttribute>>()["Pay"].Value);
        Assert.Equal("Pay", provider.GetRequiredService<IStringLocalizer<SharedResources>>()["Pay"].Value);
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
