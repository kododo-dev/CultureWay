using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.UpdateTranslations;

internal record UpdateTranslations(
    TranslationDto[] Translations,
    TranslationKeyDto[] KeysToDelete) : IRequest<string[]>;
