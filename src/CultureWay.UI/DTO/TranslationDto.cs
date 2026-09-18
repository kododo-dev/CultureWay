namespace Kododo.CultureWay.UI.DTO;

public record TranslationDto(
    string Key,
    string Culture,
    string Value,
    bool HasExternalDefault = false,
    string? ExternalDefaultValue = null);
