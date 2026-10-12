using System.Globalization;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Localization;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class ResxReadOnlySourceTests
{
    private sealed class FakeLocalizer(
        Dictionary<string, Dictionary<string, string>> stringsByCulture,
        bool throwOnRead = false) : IStringLocalizer
    {
        public bool? IncludeParentCulturesArg { get; private set; }

        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            IncludeParentCulturesArg = includeParentCultures;
            if (throwOnRead) throw new InvalidOperationException("boom");

            var culture = CultureInfo.CurrentUICulture.Name;
            return stringsByCulture.TryGetValue(culture, out var strings)
                ? strings.Select(kv => new LocalizedString(kv.Key, kv.Value)).ToList()
                : [];
        }
    }

    private static Dictionary<string, Dictionary<string, string>> Sample() => new()
    {
        ["en"] = new() { ["Title"] = "Hello", ["Bye"] = "Goodbye" },
        ["pl"] = new() { ["Title"] = "Czesc" },
    };

    private static CultureWayOptions Options(params string[] cultures)
        => new() { SupportedCultures = cultures, DefaultCulture = cultures[0] };

    [Fact]
    public async Task GetDefaults_ReturnsStringsForEverySupportedCulture()
    {
        var source = new ResxReadOnlySource(new FakeLocalizer(Sample()), "", Options("en", "pl"));

        var result = await source.GetDefaultsAsync();

        Assert.Equivalent(new[]
        {
            new Translation("Title", "en", "Hello"),
            new Translation("Bye", "en", "Goodbye"),
            new Translation("Title", "pl", "Czesc"),
        }, result, strict: true);
    }

    [Fact]
    public async Task GetDefaults_WithPrefix_PrependsPrefixToKeys()
    {
        var source = new ResxReadOnlySource(new FakeLocalizer(Sample()), "Home", Options("pl"));

        var result = await source.GetDefaultsAsync();

        Assert.Equal("Home.Title", Assert.Single(result).Key);
    }

    [Fact]
    public async Task GetDefaults_WithEmptyPrefix_UsesRawKeys()
    {
        var source = new ResxReadOnlySource(new FakeLocalizer(Sample()), "", Options("pl"));

        var result = await source.GetDefaultsAsync();

        Assert.Equal("Title", Assert.Single(result).Key);
    }

    [Fact]
    public async Task GetDefaults_CultureWithoutResources_ContributesNothing()
    {
        var source = new ResxReadOnlySource(new FakeLocalizer(Sample()), "", Options("de"));

        var result = await source.GetDefaultsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDefaults_AsksLocalizerToIncludeParentCultures()
    {
        var localizer = new FakeLocalizer(Sample());
        var source = new ResxReadOnlySource(localizer, "", Options("en"));

        await source.GetDefaultsAsync();

        Assert.True(localizer.IncludeParentCulturesArg);
    }

    [Fact]
    public async Task GetDefaults_RestoresCurrentUICulture()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
            var source = new ResxReadOnlySource(new FakeLocalizer(Sample()), "", Options("en", "pl"));

            await source.GetDefaultsAsync();

            Assert.Equal("de", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public async Task GetDefaults_LocalizerThrows_StillRestoresCurrentUICulture()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
            var source = new ResxReadOnlySource(new FakeLocalizer(Sample(), throwOnRead: true), "", Options("en"));

            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => source.GetDefaultsAsync());
            Assert.Equal("de", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
