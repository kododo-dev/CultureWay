using System.Diagnostics.CodeAnalysis;
using Kododo.CultureWay.Core.Model;

namespace Kododo.CultureWay.Localization;

internal sealed class TranslationCache
{
    private volatile IReadOnlyDictionary<(string Key, string Culture), string> _data =
        new Dictionary<(string Key, string Culture), string>();

    internal void Load(IEnumerable<Translation> translations)
    {
        _data = translations.ToDictionary(t => (t.Key, t.Culture), t => t.Value);
    }

    internal bool TryGetValue(string key, string culture, [NotNullWhen(true)] out string? value)
        => _data.TryGetValue((key, culture), out value);

    internal IReadOnlyCollection<Translation> GetAll()
        => _data.Select(kv => new Translation(kv.Key.Key, kv.Key.Culture, kv.Value)).ToList();
}
