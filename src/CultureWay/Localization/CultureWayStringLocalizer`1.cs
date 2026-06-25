using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

internal sealed class CultureWayStringLocalizer<T>(IStringLocalizerFactory factory) : IStringLocalizer<T>
{
    private readonly IStringLocalizer _inner = factory.Create(typeof(T));

    public LocalizedString this[string name] => _inner[name];
    public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => _inner.GetAllStrings(includeParentCultures);
}
