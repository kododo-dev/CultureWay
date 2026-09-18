using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Kododo.CultureWay.UI.API.DeleteCulture;

internal sealed class DeleteCultureHandler(
    IStore store,
    TranslationCache cache,
    CultureWayOptions options,
    IOptions<RequestLocalizationOptions> requestLocalizationOptions) : IRequestHandler<DeleteCulture, string[]>
{
    public async Task<string[]> HandleAsync(DeleteCulture request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Culture))
            return ["Culture code must not be empty."];

        var match = Array.Find(options.SupportedCultures,
            c => string.Equals(c, request.Culture, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            return [$"Culture '{request.Culture}' is not supported."];

        if (string.Equals(match, options.DefaultCulture, StringComparison.OrdinalIgnoreCase))
            return ["The default culture cannot be deleted."];

        if (options.SupportedCultures.Length <= 1)
            return ["At least one culture must remain."];

        options.SupportedCultures = [.. options.SupportedCultures.Where(c => c != match)];
        await store.RemoveSupportedCultureAsync(match, cancellationToken);

        var all = await store.GetAllAsync(cancellationToken);
        var keysToDelete = all
            .Where(t => string.Equals(t.Culture, match, StringComparison.OrdinalIgnoreCase))
            .Select(t => (t.Key, t.Culture))
            .ToArray();

        if (keysToDelete.Length > 0)
            await store.DeleteAsync(keysToDelete, cancellationToken);

        var remaining = all.Where(t => !string.Equals(t.Culture, match, StringComparison.OrdinalIgnoreCase)).ToList();
        cache.Load(remaining);

        RequestLocalizationSync.Remove(requestLocalizationOptions.Value, match);

        return [];
    }
}
