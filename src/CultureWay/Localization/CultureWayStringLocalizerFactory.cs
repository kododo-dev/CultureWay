using System.Collections.Concurrent;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

internal sealed class CultureWayStringLocalizerFactory(TranslationCache cache, CultureWayOptions options) : IStringLocalizerFactory
{
    private readonly ConcurrentDictionary<string, CultureWayStringLocalizer> _localizers = new(StringComparer.Ordinal);

    public IStringLocalizer Create(Type resourceSource) => ForScope(options.ScopeOf(resourceSource));

    // Used by IViewLocalizer and by resources named by string. The base name carries the assembly name as a
    // prefix (e.g. "MyApp.Views.Home.Index"), which is dropped so the scope reads "Views.Home.Index".
    public IStringLocalizer Create(string baseName, string location)
    {
        var scope = !string.IsNullOrEmpty(location) && baseName.StartsWith(location + ".", StringComparison.Ordinal)
            ? baseName[(location.Length + 1)..]
            : baseName;
        return ForScope(scope);
    }

    private CultureWayStringLocalizer ForScope(string? scope)
    {
        var normalized = scope?.Trim('.') ?? "";
        return _localizers.GetOrAdd(normalized, s => new CultureWayStringLocalizer(cache, options, s));
    }
}
