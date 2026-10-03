namespace Kododo.CultureWay.UI.DTO;

public record EditorSettingsDto(
    string? Title,
    string? HomeUrl,
    string? Language,
    EditorLinkDto[] Links,
    EditorUserDto? User,
    bool CanManageCultures);

public record EditorLinkDto(string Label, string Url);

public record EditorUserDto(string Name, string? AccountUrl, string? SignOutUrl);
