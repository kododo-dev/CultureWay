using System.Globalization;
using Kododo.CultureWay.Core.Store;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Kododo.CultureWay.UI.API.AddCulture;

internal sealed class AddCultureHandler(
    IStore store,
    CultureWayOptions options,
    IOptions<RequestLocalizationOptions> requestLocalizationOptions) : IRequestHandler<AddCulture, string[]>
{
    public async Task<string[]> HandleAsync(AddCulture request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Culture))
            return ["Culture code must not be empty."];

        CultureInfo cultureInfo;
        try
        {
            cultureInfo = CultureInfo.GetCultureInfo(request.Culture.Trim());
        }
        catch (CultureNotFoundException)
        {
            return [$"'{request.Culture}' is not a valid culture code."];
        }

        if (options.SupportedCultures.Contains(cultureInfo.Name, StringComparer.OrdinalIgnoreCase))
            return [$"Culture '{cultureInfo.Name}' is already supported."];

        options.SupportedCultures = [.. options.SupportedCultures, cultureInfo.Name];
        await store.AddSupportedCultureAsync(cultureInfo.Name, cancellationToken);
        RequestLocalizationSync.Sync(requestLocalizationOptions.Value, [cultureInfo.Name]);

        return [];
    }
}
