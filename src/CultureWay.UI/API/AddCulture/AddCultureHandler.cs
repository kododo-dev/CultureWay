using System.Globalization;
using Kododo.CultureWay.Core.Store;
using Kododo.Reiho.AspNetCore.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Kododo.CultureWay.UI.API.AddCulture;

internal sealed class AddCultureHandler(
    IStore store,
    CultureWayOptions options,
    IOptions<RequestLocalizationOptions> requestLocalizationOptions) : IRequestHandler<AddCulture>
{
    public async Task HandleAsync(AddCulture request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Culture))
            throw RequestException.Invalid("culture", "Culture code must not be empty.");

        CultureInfo cultureInfo;
        try
        {
            cultureInfo = CultureInfo.GetCultureInfo(request.Culture.Trim());
        }
        catch (CultureNotFoundException)
        {
            throw RequestException.Invalid("culture", $"'{request.Culture}' is not a valid culture code.");
        }

        if (options.SupportedCultures.Contains(cultureInfo.Name, StringComparer.OrdinalIgnoreCase))
            throw RequestException.Conflict($"Culture '{cultureInfo.Name}' is already supported.");

        options.SupportedCultures = [.. options.SupportedCultures, cultureInfo.Name];
        await store.AddSupportedCultureAsync(cultureInfo.Name, cancellationToken);
        RequestLocalizationSync.Sync(requestLocalizationOptions.Value, [cultureInfo.Name]);
    }
}
