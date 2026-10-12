using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

/// <summary>
/// Resolves keys from the translation cache. A localizer with a scope reads <c>{scope}.{name}</c> and, when
/// <see cref="CultureWayOptions.FallBackToUnscopedKeys"/> is set, falls back to <c>{name}</c>.
/// </summary>
internal sealed class CultureWayStringLocalizer(TranslationCache cache, CultureWayOptions options, string scope = "")
    : IStringLocalizer
{
    private readonly string? _prefix = scope.Length > 0 ? scope + "." : null;

    public LocalizedString this[string name]
    {
        get
        {
            var value = Resolve(name);
            return value is not null
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true, searchedLocation: ScopedKey(name));
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = Resolve(name);
            return value is not null
                ? new LocalizedString(name, string.Format(value, arguments))
                : new LocalizedString(name, string.Format(name, arguments), resourceNotFound: true, searchedLocation: ScopedKey(name));
        }
    }

    /// <summary>
    /// Every key of the scope, with the scope prefix removed. An unscoped localizer returns every key.
    /// </summary>
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
                if (!string.Equals(t.Culture, culture, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (_prefix is null)
                {
                    if (seen.Add(t.Key))
                        yield return new LocalizedString(t.Key, t.Value);
                }
                else if (t.Key.StartsWith(_prefix, StringComparison.Ordinal) && t.Key.Length > _prefix.Length)
                {
                    var name = t.Key[_prefix.Length..];
                    if (seen.Add(name))
                        yield return new LocalizedString(name, t.Value);
                }
            }
        }
    }

    private string ScopedKey(string name) => _prefix is null ? name : _prefix + name;

    private string? Resolve(string name)
    {
        var scopedKey = _prefix is null ? null : _prefix + name;
        var tryUnscoped = scopedKey is null || options.FallBackToUnscopedKeys;

        // The user's language matters more than the scope, so each culture gets both keys before the next one.
        foreach (var culture in GetCulturesToTry())
        {
            if (scopedKey is not null && cache.TryGetValue(scopedKey, culture, out var scoped))
                return scoped;

            if (tryUnscoped && cache.TryGetValue(name, culture, out var unscoped))
                return unscoped;
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
