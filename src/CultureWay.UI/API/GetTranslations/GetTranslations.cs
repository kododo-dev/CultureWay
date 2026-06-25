using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.GetTranslations;

internal record GetTranslations : IRequest<TranslationDto[]>;
