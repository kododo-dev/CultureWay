using System.Globalization;
using FluentAssertions;
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

        options.SupportedCultures!.Select(c => c.Name).Should().BeEquivalentTo(["en", "fr"]);
        options.SupportedUICultures!.Select(c => c.Name).Should().BeEquivalentTo(["en", "fr"]);
    }

    [Fact]
    public void Sync_ExistingCulture_NotDuplicated()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Sync(options, ["pl", "pl"]);

        options.SupportedCultures!.Count(c => c.Name == "pl").Should().Be(1);
        options.SupportedUICultures!.Count(c => c.Name == "pl").Should().Be(1);
    }

    [Fact]
    public void Sync_InvalidCultureCode_IsSkipped()
    {
        var options = OptionsWith("en");

        var act = () => RequestLocalizationSync.Sync(options, ["not-a-culture-!!", "de"]);

        act.Should().NotThrow();
        options.SupportedCultures!.Select(c => c.Name).Should().BeEquivalentTo(["en", "de"]);
    }

    [Fact]
    public void Sync_NullCultureLists_DoesNotThrow()
    {
        var options = new RequestLocalizationOptions { SupportedCultures = null, SupportedUICultures = null };

        var act = () => RequestLocalizationSync.Sync(options, ["fr"]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Remove_ExistingCulture_RemovedFromBothLists()
    {
        var options = OptionsWith("en", "fr");

        RequestLocalizationSync.Remove(options, "fr");

        options.SupportedCultures!.Select(c => c.Name).Should().Equal("en");
        options.SupportedUICultures!.Select(c => c.Name).Should().Equal("en");
    }

    [Fact]
    public void Remove_IsCaseInsensitive()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Remove(options, "PL");

        options.SupportedCultures!.Select(c => c.Name).Should().Equal("en");
    }

    [Fact]
    public void Remove_UnknownCulture_LeavesListsUnchanged()
    {
        var options = OptionsWith("en", "pl");

        RequestLocalizationSync.Remove(options, "de");

        options.SupportedCultures!.Select(c => c.Name).Should().BeEquivalentTo(["en", "pl"]);
        options.SupportedUICultures!.Select(c => c.Name).Should().BeEquivalentTo(["en", "pl"]);
    }

    [Fact]
    public void Remove_NullCultureLists_DoesNotThrow()
    {
        var options = new RequestLocalizationOptions { SupportedCultures = null, SupportedUICultures = null };

        var act = () => RequestLocalizationSync.Remove(options, "en");

        act.Should().NotThrow();
    }
}
