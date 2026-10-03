using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Http;

namespace Kododo.CultureWay.UI.API.GetEditorSettings;

internal sealed class GetEditorSettingsHandler(EditorOptions options, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetEditorSettings, EditorSettingsDto>
{
    public Task<EditorSettingsDto> HandleAsync(GetEditorSettings request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("GetEditorSettings must run inside an HTTP request.");

        var links = options.Links?.Invoke(context)
            .Select(link => new EditorLinkDto(link.Label, link.Url))
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
            options.AllowsCultureManagement(context)));
    }
}
