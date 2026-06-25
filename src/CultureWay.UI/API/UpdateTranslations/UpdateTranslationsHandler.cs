using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.UpdateTranslations;

internal sealed class UpdateTranslationsHandler(
    IStore store,
    TranslationCache cache,
    CultureWayOptions options) : IRequestHandler<UpdateTranslations, string[]>
{
    public async Task<string[]> HandleAsync(UpdateTranslations request, CancellationToken cancellationToken)
    {
        var errors = Validate(request.Translations, request.KeysToDelete);
        if (errors.Length > 0)
            return errors;

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

        return [];
    }

    private string[] Validate(TranslationDto[] translations, TranslationKeyDto[] keysToDelete)
    {
        var errors = new List<string>();
        var supported = new HashSet<string>(options.SupportedCultures, StringComparer.OrdinalIgnoreCase);

        foreach (var t in translations)
        {
            if (string.IsNullOrWhiteSpace(t.Key))
                errors.Add("Translation key must not be empty.");
            else if (string.IsNullOrWhiteSpace(t.Culture))
                errors.Add($"{t.Key}: culture must not be empty.");
            else if (!supported.Contains(t.Culture))
                errors.Add($"{t.Key}: culture '{t.Culture}' is not in SupportedCultures.");
            else if (t.Value is null)
                errors.Add($"{t.Key} [{t.Culture}]: value must not be null.");
        }

        foreach (var k in keysToDelete)
        {
            if (string.IsNullOrWhiteSpace(k.Key))
                errors.Add("Delete key must not be empty.");
            else if (string.IsNullOrWhiteSpace(k.Culture))
                errors.Add($"{k.Key}: culture to delete must not be empty.");
        }

        return [.. errors];
    }
}
