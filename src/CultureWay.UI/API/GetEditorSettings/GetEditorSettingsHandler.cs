using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Http;

namespace Kododo.CultureWay.UI.API.GetEditorSettings;

// The editor's frame (title, user, language) comes from Way.Shell now; the obsolete values are still reported for
// clients of this API. The editor itself uses the permissions, the import limit and the per-request links.
#pragma warning disable CS0618
internal sealed class GetEditorSettingsHandler(EditorOptions options, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetEditorSettings, EditorSettingsDto>
{
    public Task<EditorSettingsDto> HandleAsync(GetEditorSettings request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("GetEditorSettings must run inside an HTTP request.");

        var links = options.Links?.Invoke(context)
            .Select(link => new EditorLinkDto(link.Label, link.Url, link.Icon))
            .ToArray() ?? [];

        var user = options.User?.Invoke(context) is { } u
            ? new EditorUserDto(u.Name, u.AccountUrl, u.SignOutUrl)
            : null;

        return Task.FromResult(new EditorSettingsDto(
            options.Title,
            options.HomeUrl,
            options.Language,
            links,
            user,
            options.AllowsCultureManagement(context),
            options.AllowsImport(context),
            options.MaxImportBytes));
    }
}
#pragma warning restore CS0618
