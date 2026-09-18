using System.Globalization;
using Microsoft.AspNetCore.Builder;

namespace Kododo.CultureWay.UI;

/// <summary>
/// Keeps ASP.NET Core's own <see cref="RequestLocalizationOptions"/> in sync with culture codes
/// added to CultureWay at runtime, so the request localization middleware recognizes them
/// without requiring an application restart.
/// </summary>
internal static class RequestLocalizationSync
{
    public static void Sync(RequestLocalizationOptions options, IEnumerable<string> cultureCodes)
    {
        foreach (var code in cultureCodes)
        {
            CultureInfo cultureInfo;
            try
            {
                cultureInfo = CultureInfo.GetCultureInfo(code);
            }
            catch (CultureNotFoundException)
            {
                continue;
            }

            if (options.SupportedCultures is not null && options.SupportedCultures.All(c => c.Name != cultureInfo.Name))
                options.SupportedCultures.Add(cultureInfo);

            if (options.SupportedUICultures is not null && options.SupportedUICultures.All(c => c.Name != cultureInfo.Name))
                options.SupportedUICultures.Add(cultureInfo);
        }
    }

    public static void Remove(RequestLocalizationOptions options, string cultureCode)
    {
        RemoveMatching(options.SupportedCultures, cultureCode);
        RemoveMatching(options.SupportedUICultures, cultureCode);
    }

    private static void RemoveMatching(IList<CultureInfo>? cultures, string cultureCode)
    {
        if (cultures is null) return;

        for (var i = cultures.Count - 1; i >= 0; i--)
        {
            if (string.Equals(cultures[i].Name, cultureCode, StringComparison.OrdinalIgnoreCase))
                cultures.RemoveAt(i);
        }
    }
}
