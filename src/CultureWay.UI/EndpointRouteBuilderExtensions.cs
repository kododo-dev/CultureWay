using Kododo.Reiho.AspNetCore.API;
using Kododo.Reiho.AspNetCore.SPA;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kododo.CultureWay.UI;

/// <summary>
/// Extension methods for mounting the CultureWay translation editor in the ASP.NET Core middleware pipeline.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Mounts the CultureWay translation editor and its API at the specified path.
        /// </summary>
        /// <param name="path">
        /// The base path for the CultureWay UI. Defaults to <c>/translations</c>.
        /// The API is available at <c>{path}/api</c> and the SPA at <c>{path}</c>.
        /// </param>
        /// <returns>
        /// A <see cref="RouteGroupBuilder"/> that can be used to apply additional
        /// middleware such as authentication or authorization policies.
        /// </returns>
        /// <remarks>
        /// Call this after <see cref="HostExtensions.InitializeCultureWayAsync"/> so that any
        /// supported cultures persisted by the store in a previous run are synced into
        /// ASP.NET Core's <see cref="RequestLocalizationOptions"/> (if configured) before the
        /// app starts serving requests.
        /// </remarks>
        /// <example>
        /// <code>
        /// app.UseCultureWay("/translations").RequireAuthorization("Admin");
        /// </code>
        /// </example>
        public RouteGroupBuilder UseCultureWay(string path = "/translations")
        {
            var group = endpoints.MapGroup(path);
            group.MapApi();
            group.MapEmbeddedSpa(typeof(EndpointRouteBuilderExtensions).Assembly);

            var cultureWayOptions = endpoints.ServiceProvider.GetService<CultureWayOptions>();
            var requestLocalizationOptions = endpoints.ServiceProvider.GetService<IOptions<RequestLocalizationOptions>>();
            if (cultureWayOptions is not null && requestLocalizationOptions is not null)
                RequestLocalizationSync.Sync(requestLocalizationOptions.Value, cultureWayOptions.SupportedCultures);

            return group;
        }

        private void MapApi()
        {
            var api = endpoints.MapGroup("/api");
            api.MapRequests(typeof(EndpointRouteBuilderExtensions).Assembly);
        }
    }
}
