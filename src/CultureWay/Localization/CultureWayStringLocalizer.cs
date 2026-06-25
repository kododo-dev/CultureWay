using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

internal sealed class CultureWayStringLocalizer(TranslationCache cache, CultureWayOptions options) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var value = Resolve(name);
            return value is not null
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Resolve(name);
            return value is not null
                ? new LocalizedString(name, string.Format(value, arguments))
                : new LocalizedString(name, string.Format(name, arguments), resourceNotFound: true);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var cultures = includeParentCultures
            ? GetCulturesToTry()
            : [CultureInfo.CurrentUICulture.Name, options.DefaultCulture];

        var all = cache.GetAll();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var culture in cultures)
        {
            foreach (var t in all)
            {
                if (string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase) && seen.Add(t.Key))
                    yield return new LocalizedString(t.Key, t.Value);
            }
        }
    }

    private string? Resolve(string name)
    {
        foreach (var culture in GetCulturesToTry())
        {
            if (cache.TryGetValue(name, culture, out var value))
                return value;
        }

        return null;
    }

    private IEnumerable<string> GetCulturesToTry()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var current = CultureInfo.CurrentUICulture;
        while (!string.IsNullOrEmpty(current.Name))
        {
            if (seen.Add(current.Name)) yield return current.Name;
            current = current.Parent;
        }

        if (seen.Add(options.DefaultCulture)) yield return options.DefaultCulture;
    }
}
