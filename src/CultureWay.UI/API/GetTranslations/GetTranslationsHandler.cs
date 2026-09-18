using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.GetTranslations;

internal sealed class GetTranslationsHandler(
    IStore store,
    IEnumerable<IReadOnlySource> readOnlySources) : IRequestHandler<GetTranslations, TranslationDto[]>
{
    public async Task<TranslationDto[]> HandleAsync(GetTranslations request, CancellationToken cancellationToken)
    {
        var storeTranslations = await store.GetAllAsync(cancellationToken);

        var storeMap = storeTranslations.ToDictionary(
            t => (t.Key, t.Culture),
            t => t.Value,
            CultureKeyComparer.Instance);

        var externalDefaults = new Dictionary<(string, string), string>(CultureKeyComparer.Instance);
        foreach (var source in readOnlySources)
        {
            var defaults = await source.GetDefaultsAsync(cancellationToken);
            foreach (var t in defaults)
                externalDefaults.TryAdd((t.Key, t.Culture), t.Value);
        }

        var result = new List<TranslationDto>(storeTranslations.Count + externalDefaults.Count);

        foreach (var t in storeTranslations)
        {
            var hasExternal = externalDefaults.TryGetValue((t.Key, t.Culture), out var defaultValue);
            result.Add(new TranslationDto(t.Key, t.Culture, t.Value, hasExternal, defaultValue));
        }

        foreach (var ((key, culture), externalValue) in externalDefaults)
        {
            if (!storeMap.ContainsKey((key, culture)))
                result.Add(new TranslationDto(key, culture, externalValue, true, externalValue));
        }

        return result
            .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Culture, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed class CultureKeyComparer : IEqualityComparer<(string Key, string Culture)>
    {
        public static readonly CultureKeyComparer Instance = new();

        public bool Equals((string Key, string Culture) x, (string Key, string Culture) y) =>
            string.Equals(x.Key, y.Key, StringComparison.Ordinal) &&
            string.Equals(x.Culture, y.Culture, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Key, string Culture) obj) =>
            HashCode.Combine(obj.Key, obj.Culture.ToLowerInvariant());
    }
}
