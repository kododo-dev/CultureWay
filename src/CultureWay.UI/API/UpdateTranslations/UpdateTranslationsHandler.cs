using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.UpdateTranslations;

internal sealed class UpdateTranslationsHandler(
    IStore store,
    TranslationCache cache,
    CultureWayOptions options) : IRequestHandler<UpdateTranslations>
{
    public async Task HandleAsync(UpdateTranslations request, CancellationToken cancellationToken)
    {
        var errors = Validate(request.Translations, request.KeysToDelete);
        if (errors.Count > 0)
            throw new RequestException("The translations are not valid.") { Errors = errors };

        if (request.KeysToDelete.Length > 0)
        {
            var keys = request.KeysToDelete
                .Select(k => (k.Key, k.Culture))
                .ToArray();
            await store.DeleteAsync(keys, cancellationToken);
        }

        if (request.Translations.Length > 0)
        {
            var translations = request.Translations
                .Select(t => new Translation(t.Key, t.Culture, t.Value))
                .ToArray();
            await store.SetAsync(translations, cancellationToken);
        }

        var all = await store.GetAllAsync(cancellationToken);
        cache.Load(all);
    }

    /// <summary>Messages per field of the request, named as in its JSON (<c>translations[2].culture</c>).</summary>
    private Dictionary<string, string[]> Validate(TranslationDto[] translations, TranslationKeyDto[] keysToDelete)
    {
        var errors = new Dictionary<string, string[]>();
        var supported = new HashSet<string>(options.SupportedCultures, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < translations.Length; i++)
        {
            var t = translations[i];
            if (string.IsNullOrWhiteSpace(t.Key))
                errors[$"translations[{i}].key"] = ["Translation key must not be empty."];
            else if (string.IsNullOrWhiteSpace(t.Culture))
                errors[$"translations[{i}].culture"] = [$"{t.Key}: culture must not be empty."];
            else if (!supported.Contains(t.Culture))
                errors[$"translations[{i}].culture"] = [$"{t.Key}: culture '{t.Culture}' is not in SupportedCultures."];
            else if (t.Value is null)
                errors[$"translations[{i}].value"] = [$"{t.Key} [{t.Culture}]: value must not be null."];
        }

        for (var i = 0; i < keysToDelete.Length; i++)
        {
            var k = keysToDelete[i];
            if (string.IsNullOrWhiteSpace(k.Key))
                errors[$"keysToDelete[{i}].key"] = ["Delete key must not be empty."];
            else if (string.IsNullOrWhiteSpace(k.Culture))
                errors[$"keysToDelete[{i}].culture"] = [$"{k.Key}: culture to delete must not be empty."];
        }

        return errors;
    }
}
