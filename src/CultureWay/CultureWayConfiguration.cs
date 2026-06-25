using Microsoft.Extensions.DependencyInjection;

namespace Kododo.CultureWay;

internal sealed class CultureWayConfiguration(IServiceCollection services) : ICultureWayConfiguration
{
    public IServiceCollection Services { get; } = services;
    public CultureWayOptions Options { get; } = new();
}
