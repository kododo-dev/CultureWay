using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.DeleteCulture;

internal record DeleteCulture(string Culture) : IRequest<string[]>;
