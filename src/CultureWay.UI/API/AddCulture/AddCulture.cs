using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.AddCulture;

internal record AddCulture(string Culture) : IRequest<string[]>;
