using Kododo.CultureWay.Core.Store;

namespace Kododo.CultureWay.UI.ImportExport;

/// <summary>
/// The values the editor shows: the store's, falling back to the read-only sources' defaults
/// (for example .resx files) for keys the store does not override.
/// </summary>
internal sealed class TranslationSnapshot
{
    private readonly Dictionary<(string Key, string Culture), string> _values;

    private TranslationSnapshot(Dictionary<(string Key, string Culture), string> values) => _values = values;

    public static async Task<TranslationSnapshot> LoadAsync(
        IStore store,
        IEnumerable<IReadOnlySource> readOnlySources,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<(string Key, string Culture), string>(KeyCultureComparer.Instance);

        foreach (var t in await store.GetAllAsync(cancellationToken))
            values[(t.Key, t.Culture)] = t.Value;

        foreach (var source in readOnlySources)
        foreach (var t in await source.GetDefaultsAsync(cancellationToken))
            values.TryAdd((t.Key, t.Culture), t.Value);

        return new TranslationSnapshot(values);
    }

    public IEnumerable<string> Keys => _values.Keys
        .Select(k => k.Key)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

    public string? Get(string key, string culture) => _values.GetValueOrDefault((key, culture));

    /// <summary>
    /// Whether a key falls under a namespace as the editor's side menu selects it: <c>null</c> is
    /// every key, an empty string the keys without a namespace, and anything else that namespace
    /// and the ones nested in it.
    /// </summary>
    public static bool InNamespace(string key, string? ns)
    {
        if (ns is null)
            return true;

        // The same rule as the editor's extractNamespace.
        var lastDot = key.LastIndexOf('.');
        var keyNamespace = lastDot > 0 ? key[..lastDot] : "";
        if (ns.Length == 0)
            return keyNamespace.Length == 0;

        return keyNamespace == ns || keyNamespace.StartsWith(ns + ".", StringComparison.Ordinal);
    }

    private sealed class KeyCultureComparer : IEqualityComparer<(string Key, string Culture)>
    {
        public static readonly KeyCultureComparer Instance = new();

        public bool Equals((string Key, string Culture) x, (string Key, string Culture) y) =>
            string.Equals(x.Key, y.Key, StringComparison.Ordinal) &&
            string.Equals(x.Culture, y.Culture, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Key, string Culture) obj) =>
            HashCode.Combine(obj.Key, obj.Culture.ToLowerInvariant());
    }
}
