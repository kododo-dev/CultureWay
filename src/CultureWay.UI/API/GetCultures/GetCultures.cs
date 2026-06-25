using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.GetCultures;

internal record GetCultures : IRequest<CultureDto[]>;
