using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.SetDefaultCulture;

internal record SetDefaultCulture(string Culture) : IRequest<string[]>;
