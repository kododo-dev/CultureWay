using Kododo.CultureWay.Core.Store;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.SetDefaultCulture;

internal sealed class SetDefaultCultureHandler(
    IStore store,
    CultureWayOptions options) : IRequestHandler<SetDefaultCulture>
{
    public async Task HandleAsync(SetDefaultCulture request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Culture))
            throw RequestException.Invalid("culture", "Culture code must not be empty.");

        var match = Array.Find(options.SupportedCultures,
            c => string.Equals(c, request.Culture, StringComparison.OrdinalIgnoreCase));

        if (match is null)
            throw RequestException.NotFound($"Culture '{request.Culture}' is not supported.");

        if (string.Equals(match, options.DefaultCulture, StringComparison.OrdinalIgnoreCase))
            return;

        options.DefaultCulture = match;
        await store.SetDefaultCultureAsync(match, cancellationToken);
    }
}
