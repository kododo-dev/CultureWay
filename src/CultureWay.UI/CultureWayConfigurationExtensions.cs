using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI;

/// <summary>
/// Extension methods for enabling the CultureWay embedded web UI.
/// </summary>
public static class CultureWayConfigurationExtensions
{
    /// <summary>
    /// Enables the CultureWay translation editor by registering the required API request handlers.
    /// Call <see cref="EndpointRouteBuilderExtensions.UseCultureWay"/> in the middleware
    /// pipeline to mount the UI at a specific path.
    /// </summary>
    public static ICultureWayConfiguration AddEditor(this ICultureWayConfiguration configuration)
    {
        configuration.Services.AddRequestHandlers(typeof(CultureWayConfigurationExtensions).Assembly);
        return configuration;
    }
}
