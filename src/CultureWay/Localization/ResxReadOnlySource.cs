using System.Globalization;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay.Localization;

/// <summary>
/// Surfaces the strings of a standard ASP.NET Core resx-based <see cref="IStringLocalizer"/>
/// (resolved via the framework's own type/namespace naming convention) as a read-only translation source.
/// </summary>
internal sealed class ResxReadOnlySource(IStringLocalizer localizer, string prefix, CultureWayOptions options) : IReadOnlySource
{
    public Task<IReadOnlyList<Translation>> GetDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<Translation>();
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            foreach (var culture in options.SupportedCultures)
            {
                // GetAllStrings() reads CultureInfo.CurrentUICulture; there is no culture-parameterized
                // overload. This flows with the async context, so it is safe across concurrent requests.
                // includeParentCultures:true mirrors ResourceManager's own fallback chain (e.g. "en" falls
                // back to the neutral/invariant resx, which is where a base "Foo.resx" without a culture
                // suffix is actually compiled) rather than requiring an exact-culture satellite to exist.
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);

                foreach (var entry in localizer.GetAllStrings(includeParentCultures: true))
                {
                    var key = string.IsNullOrEmpty(prefix) ? entry.Name : $"{prefix}.{entry.Name}";
                    result.Add(new Translation(key, culture, entry.Value));
                }
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.FromResult<IReadOnlyList<Translation>>(result);
    }
}
