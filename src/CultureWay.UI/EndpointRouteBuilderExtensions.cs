using Kododo.CultureWay.UI.API.AddCulture;
using Kododo.CultureWay.UI.API.ApplyImport;
using Kododo.CultureWay.UI.API.DeleteCulture;
using Kododo.CultureWay.UI.API.PreviewImport;
using Kododo.CultureWay.UI.API.SetDefaultCulture;
using Kododo.Reiho.AspNetCore.API;
using Kododo.Way.Shell;
using Microsoft.AspNetCore.Http;
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
    /// <summary>The editor as a Way.Shell module: <c>SPA/dist</c> is built with the wayModule() preset.</summary>
    internal static readonly WayModule Module =
        new("translations", "Translations", typeof(EndpointRouteBuilderExtensions).Assembly, "SPA/dist") { Icon = "translate" };

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Mounts the CultureWay translation editor and its API at the specified path, in Way.Shell.
        /// </summary>
        /// <param name="path">
        /// The base path for the CultureWay UI. Defaults to <c>/translations</c>.
        /// The API is available at <c>{path}/api</c> and the SPA at <c>{path}</c>.
        /// </param>
        /// <returns>
        /// A <see cref="RouteGroupBuilder"/> that can be used to apply additional
        /// middleware such as authentication or authorization policies.
        /// Outside the Development environment the editor and its API answer <c>403</c> until authorization is
        /// required here, anonymous access is allowed explicitly (<c>AllowAnonymous()</c>) or
        /// <c>ShellOptions.AllowAnonymous</c> is set.
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
            var group = endpoints.MapWay(Module, path);
            group.MapApi();

            var cultureWayOptions = endpoints.ServiceProvider.GetService<CultureWayOptions>();
            var requestLocalizationOptions = endpoints.ServiceProvider.GetService<IOptions<RequestLocalizationOptions>>();
            if (cultureWayOptions is not null && requestLocalizationOptions is not null)
                RequestLocalizationSync.Sync(requestLocalizationOptions.Value, cultureWayOptions.SupportedCultures);

            return group;
        }

        private void MapApi()
        {
            var api = endpoints.MapGroup("/api");
            api.AddEndpointFilter(RequirePermission);
            api.MapRequests(typeof(EndpointRouteBuilderExtensions).Assembly);
        }
    }

    private static readonly HashSet<string> CultureManagementRequests =
        new([nameof(AddCulture), nameof(DeleteCulture), nameof(SetDefaultCulture)], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ImportRequests =
        new([nameof(PreviewImport), nameof(ApplyImport)], StringComparer.OrdinalIgnoreCase);

    // Reiho maps each request at its type name, so the last path segment says which request this is.
    private static async ValueTask<object?> RequirePermission(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var request = http.Request.Path.Value?.Split('/').LastOrDefault();
        var editorOptions = http.RequestServices.GetService<EditorOptions>();
        if (request is not null && editorOptions is not null)
        {
            var allowed = true;
            if (CultureManagementRequests.Contains(request))
                allowed = editorOptions.AllowsCultureManagement(http);
            else if (ImportRequests.Contains(request))
                allowed = editorOptions.AllowsImport(http);

            if (!allowed)
                return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
