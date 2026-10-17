using Kododo.CultureWay.UI.ImportExport;
using Kododo.Reiho.AspNetCore.API;
using Kododo.Way.Shell;
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
        configuration.Services.AddScoped<ImportPlanner>();
        configuration.Services.AddRequestHandlers(typeof(CultureWayConfigurationExtensions).Assembly);

        // The editor runs in Way.Shell; what EditorOptions used to set for the editor's frame now belongs to the shell.
        // Only the values set here are passed on, so the host's own AddWayShell settings stay unless it set both.
        configuration.Services.AddWayShell(shell => ForwardToShell(editorOptions, shell));
        return configuration;
    }

#pragma warning disable CS0618 // passes the obsolete EditorOptions on to ShellOptions
    private static void ForwardToShell(EditorOptions editor, ShellOptions shell)
    {
        if (editor.Title is not null) shell.Title = editor.Title;
        if (editor.HomeUrl is not null) shell.HomeUrl = editor.HomeUrl;
        if (editor.Language is not null) shell.Language = editor.Language;
        if (editor.User is { } user)
            shell.User = context => user(context) is { } u
                ? new ShellUser(u.Name) { AccountUrl = u.AccountUrl, SignOutUrl = u.SignOutUrl }
                : null;
        // The icon names of EditorLinkIcons are icon names of the shell too.
        if (editor.Links is { } links)
            shell.DynamicLinks.Add(context => links(context).Select(l => new ShellLink(l.Label, l.Url) { Icon = l.Icon }));
    }
#pragma warning restore CS0618
}
