using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

internal sealed class CultureWayStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly CultureWayStringLocalizer _localizer;

    public CultureWayStringLocalizerFactory(TranslationCache cache, CultureWayOptions options)
    {
        _localizer = new CultureWayStringLocalizer(cache, options);
    }

    // All types share a single flat key space — no per-type scoping.
    public IStringLocalizer Create(Type resourceSource) => _localizer;
    public IStringLocalizer Create(string baseName, string location) => _localizer;
}
