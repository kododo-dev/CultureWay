using System.Globalization;
using Kododo.CultureWay.UI;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class RequestLocalizationSyncTests
{
    private static RequestLocalizationOptions OptionsWith(params string[] cultures)
    {
        var infos = cultures.Select(CultureInfo.GetCultureInfo).ToList();
        return new RequestLocalizationOptions { SupportedCultures = infos, SupportedUICultures = [.. infos] };
    }

    [Fact]
    public void Sync_NewCulture_AddedToBothLists()
    {
        var options = OptionsWith("en");

        RequestLocalizationSync.Sync(options, ["fr"]);

        Assert.Equivalent(new[] { "en", "fr" }, options.SupportedCultures!.Select(c => c.Name), strict: true);
        Assert.Equivalent(new[] { "en", "fr" }, options.SupportedUICultures!.Select(c => c.Name), strict: true);
    }

    [Fact]
    public void Sync_ExistingCulture_NotDuplicated()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Sync(options, ["pl", "pl"]);

        Assert.Equal(1, options.SupportedCultures!.Count(c => c.Name == "pl"));
        Assert.Equal(1, options.SupportedUICultures!.Count(c => c.Name == "pl"));
    }

    [Fact]
    public void Sync_InvalidCultureCode_IsSkipped()
    {
        var options = OptionsWith("en");

        RequestLocalizationSync.Sync(options, ["not-a-culture-!!", "de"]);
        Assert.Equivalent(new[] { "en", "de" }, options.SupportedCultures!.Select(c => c.Name), strict: true);
    }

    [Fact]
    public void Sync_NullCultureLists_DoesNotThrow()
    {
        var options = new RequestLocalizationOptions { SupportedCultures = null, SupportedUICultures = null };

        RequestLocalizationSync.Sync(options, ["fr"]);
    }

    [Fact]
    public void Remove_ExistingCulture_RemovedFromBothLists()
    {
        var options = OptionsWith("en", "fr");

        RequestLocalizationSync.Remove(options, "fr");

        Assert.Equal(new[] { "en" }, options.SupportedCultures!.Select(c => c.Name));
        Assert.Equal(new[] { "en" }, options.SupportedUICultures!.Select(c => c.Name));
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Remove(options, "PL");

        Assert.Equal(new[] { "en" }, options.SupportedCultures!.Select(c => c.Name));
    }

    [Fact]
    public void Remove_UnknownCulture_LeavesListsUnchanged()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Remove(options, "de");

        Assert.Equivalent(new[] { "en", "pl" }, options.SupportedCultures!.Select(c => c.Name), strict: true);
        Assert.Equivalent(new[] { "en", "pl" }, options.SupportedUICultures!.Select(c => c.Name), strict: true);
    }

    [Fact]
    public void Remove_NullCultureLists_DoesNotThrow()
    {
        var options = new RequestLocalizationOptions { SupportedCultures = null, SupportedUICultures = null };

        RequestLocalizationSync.Remove(options, "en");
    }
}
