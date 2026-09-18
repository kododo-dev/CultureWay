using Kododo.Reiho.AspNetCore.API;
using Microsoft.Extensions.DependencyInjection;

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
        // Guarantees IOptions<RequestLocalizationOptions> resolves (as a harmless default
        // instance) even for hosts that never call AddRequestLocalization themselves, so
        // AddCultureHandler can always sync newly-added cultures into it.
        configuration.Services.AddOptions();
        configuration.Services.AddRequestHandlers(typeof(CultureWayConfigurationExtensions).Assembly);
        return configuration;
    }
}
