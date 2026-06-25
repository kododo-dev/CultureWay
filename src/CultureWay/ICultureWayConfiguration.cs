using Microsoft.Extensions.DependencyInjection;

namespace Kododo.CultureWay;

public interface ICultureWayConfiguration
{
    IServiceCollection Services { get; }
    CultureWayOptions Options { get; }
}
