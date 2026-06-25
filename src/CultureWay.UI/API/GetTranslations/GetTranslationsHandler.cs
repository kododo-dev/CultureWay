using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.GetTranslations;

internal sealed class GetTranslationsHandler(IStore store) : IRequestHandler<GetTranslations, TranslationDto[]>
{
    public async Task<TranslationDto[]> HandleAsync(GetTranslations request, CancellationToken cancellationToken)
    {
        var translations = await store.GetAllAsync(cancellationToken);
        return translations
            .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Culture, StringComparer.OrdinalIgnoreCase)
            .Select(t => new TranslationDto(t.Key, t.Culture, t.Value))
            .ToArray();
    }
}
