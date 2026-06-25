using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.GetCultures;

internal sealed class GetCulturesHandler(CultureWayOptions options) : IRequestHandler<GetCultures, CultureDto[]>
{
    public Task<CultureDto[]> HandleAsync(GetCultures request, CancellationToken cancellationToken)
    {
        var cultures = options.SupportedCultures
            .Select(code => new CultureDto(code, code == options.DefaultCulture))
            .ToArray();

        return Task.FromResult(cultures);
    }
}
