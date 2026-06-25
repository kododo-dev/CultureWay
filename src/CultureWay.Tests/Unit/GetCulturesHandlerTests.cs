using FluentAssertions;
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

        result.Should().HaveCount(3);
        result.Select(c => c.Code).Should().BeEquivalentTo("en", "pl", "de");
    }

    [Fact]
    public async Task Handle_DefaultCultureHasIsDefaultTrue()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl"], DefaultCulture = "en" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        result.Single(c => c.Code == "en").IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonDefaultCulturesHaveIsDefaultFalse()
    {
        var options = new CultureWayOptions { SupportedCultures = ["en", "pl", "de"], DefaultCulture = "en" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        result.Where(c => c.Code != "en")
              .Should().AllSatisfy(c => c.IsDefault.Should().BeFalse());
    }

    [Fact]
    public async Task Handle_SingleCulture_IsDefault()
    {
        var options = new CultureWayOptions { SupportedCultures = ["pl"], DefaultCulture = "pl" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        result.Single().IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PreservesOrderFromOptions()
    {
        var options = new CultureWayOptions { SupportedCultures = ["pl", "en", "de"], DefaultCulture = "pl" };
        var handler = new GetCulturesHandler(options);

        var result = await handler.HandleAsync(new GetCultures(), CancellationToken.None);

        result.Select(c => c.Code).Should().Equal("pl", "en", "de");
    }
}
