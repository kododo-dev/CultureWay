using Kododo.CultureWay.UI.API.GetCultures;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class GetCulturesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCulturesFromOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl", "de"], DefaultCulture = "en" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        Assert.Equal(3, result.Count());
        Assert.Equivalent(new[] { "en", "pl", "de" }, result.Select(c => c.Code), strict: true);
    }

    [Fact]
    public async Task Handle_DefaultCultureHasIsDefaultTrue()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl"], DefaultCulture = "en" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        Assert.True(result.Single(c => c.Code == "en").IsDefault);
    }

    [Fact]
    public async Task Handle_NonDefaultCulturesHaveIsDefaultFalse()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl", "de"], DefaultCulture = "en" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        Assert.All(result.Where(c => c.Code != "en"), c => Assert.False(c.IsDefault));
    }

    [Fact]
    public async Task Handle_SingleCulture_IsDefault()
    {
        var options = new CultureWayOptions { SupportedCultures = ["pl"], DefaultCulture = "pl" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        Assert.True(result.Single().IsDefault);
    }

    [Fact]
    public async Task Handle_PreservesOrderFromOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["pl", "en", "de"], DefaultCulture = "pl" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        Assert.Equal(new[] { "pl", "en", "de" }, result.Select(c => c.Code));
    }
}
