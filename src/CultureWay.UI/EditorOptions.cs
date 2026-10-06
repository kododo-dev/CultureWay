using Microsoft.AspNetCore.Http;

namespace Kododo.CultureWay.UI;

/// <summary>
/// Lets the host application fit the translation editor into its own UI. Every setting is optional;
/// with none set, the editor looks and behaves as a standalone page.
/// </summary>
/// <remarks>
/// Settings that take an <see cref="HttpContext"/> are evaluated on every request, so they can depend
/// on the signed-in user (for example, showing admin links only to admins).
/// </remarks>
public sealed class EditorOptions
{
    /// <summary>
    /// The name shown at the top of the editor and in the browser tab. Defaults to <c>Translations</c>.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Where the title links to, typically the host application's home page. No link when empty.
    /// </summary>
    public string? HomeUrl { get; set; }

    /// <summary>
    /// The language of the editor's own interface (<c>en</c> or <c>pl</c>). When empty, the editor
    /// follows the browser's language and falls back to English.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Links to other pages of the host application, shown in the editor's side menu.
    /// </summary>
    public Func<HttpContext, IEnumerable<EditorLink>>? Links { get; set; }

    /// <summary>
    /// The signed-in user, shown in the editor's side menu. Return <c>null</c> to show no user.
    /// </summary>
    public Func<HttpContext, EditorUser?>? User { get; set; }

    /// <summary>
    /// Whether the current user may add and delete languages and change the default one. When it
    /// returns <c>false</c>, the editor hides language management and the API answers those
    /// requests with 403. Everyone may manage languages when this is not set.
    /// </summary>
    public Func<HttpContext, bool>? CanManageCultures { get; set; }

    internal bool AllowsCultureManagement(HttpContext context) => CanManageCultures?.Invoke(context) ?? true;
}

/// <summary>
/// A link to a page of the host application. The link to the page the editor is on is highlighted.
/// </summary>
/// <param name="Label">The text of the link.</param>
/// <param name="Url">Where it goes.</param>
public sealed record EditorLink(string Label, string Url)
{
    /// <summary>
    /// The icon shown before the label: <see cref="EditorLinkIcons.Home"/>, <see cref="EditorLinkIcons.Translations"/>,
    /// <see cref="EditorLinkIcons.Users"/> or <see cref="EditorLinkIcons.Key"/>. Any other value, or none, shows an arrow.
    /// </summary>
    public string? Icon { get; init; }
}

/// <summary>
/// The icons an <see cref="EditorLink"/> can show.
/// </summary>
public static class EditorLinkIcons
{
    public const string Home = "home";
    public const string Translations = "translations";
    public const string Users = "users";
    public const string Key = "key";
    public const string History = "history";
}

/// <summary>
/// The signed-in user as the editor shows them.
/// </summary>
/// <param name="Name">The name to display.</param>
public sealed record EditorUser(string Name)
{
    /// <summary>
    /// A page where the user manages their account. The name links to it when set.
    /// </summary>
    public string? AccountUrl { get; init; }

    /// <summary>
    /// The sign-out endpoint. The editor submits a form POST to it (without an antiforgery token),
    /// so the endpoint must accept a plain POST. No sign-out button is shown when empty.
    /// </summary>
    public string? SignOutUrl { get; init; }
}
