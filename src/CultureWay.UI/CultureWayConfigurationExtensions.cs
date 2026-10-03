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
        => configuration.AddEditor(_ => { });

    /// <summary>
    /// Enables the CultureWay translation editor and fits it into the host application's UI.
    /// See <see cref="EditorOptions"/> for what can be set.
    /// </summary>
    /// <example>
    /// <code>
    /// x.AddEditor(editor =>
    /// {
    ///     editor.Title = "My app";
    ///     editor.CanManageCultures = ctx => ctx.User.IsInRole("Admin");
    /// });
    /// </code>
    /// </example>
    public static ICultureWayConfiguration AddEditor(
        this ICultureWayConfiguration configuration,
        Action<EditorOptions> configure)
    {
        var editorOptions = new EditorOptions();
        configure(editorOptions);

        // Guarantees IOptions<RequestLocalizationOptions> resolves (as a harmless default
        // instance) even for hosts that never call AddRequestLocalization themselves, so
        // AddCultureHandler can always sync newly-added cultures into it.
        configuration.Services.AddOptions();
        configuration.Services.AddHttpContextAccessor();
        configuration.Services.AddSingleton(editorOptions);
        configuration.Services.AddRequestHandlers(typeof(CultureWayConfigurationExtensions).Assembly);
        return configuration;
    }
}
