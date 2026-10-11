namespace Kododo.CultureWay.UI.DTO;

/// <summary>A file for the browser to save, base64-encoded because the API speaks JSON.</summary>
/// <param name="Error">Why nothing was exported. The other fields are empty when set.</param>
/// <param name="FileName">The name to save the file under.</param>
/// <param name="ContentType">The file's media type.</param>
/// <param name="ContentBase64">The file.</param>
public record ExportFileDto(string? Error, string FileName, string ContentType, string ContentBase64)
{
    public static ExportFileDto Failed(string error) => new(error, "", "", "");
}

/// <summary>What an import would do, or why it cannot run.</summary>
/// <param name="Error">Why the file cannot be imported at all. Everything else is empty when set.</param>
/// <param name="Changes">The values the import adds or changes.</param>
/// <param name="Unchanged">Values in the file that already match.</param>
/// <param name="Kept">Existing values left as they are because only missing ones are imported.</param>
/// <param name="Problems">Rows or keys of the file that are skipped, and why.</param>
/// <param name="UnknownCultures">Languages in the file that the editor does not have.</param>
public record ImportPreviewDto(
    string? Error,
    ImportChangeDto[] Changes,
    int Unchanged,
    int Kept,
    ImportProblemDto[] Problems,
    string[] UnknownCultures);

/// <summary>One value an import adds or changes.</summary>
/// <param name="Key">The translation key.</param>
/// <param name="Culture">The language.</param>
/// <param name="OldValue">The current value, or <c>null</c> when the import adds it.</param>
/// <param name="NewValue">The value from the file.</param>
public record ImportChangeDto(string Key, string Culture, string? OldValue, string NewValue);

/// <summary>A part of an import file that is skipped.</summary>
/// <param name="Location">The row (Excel) or key (JSON) in the file.</param>
/// <param name="Reason">Why it is skipped.</param>
public record ImportProblemDto(string Location, string Reason);

/// <summary>What an import did.</summary>
/// <param name="Error">Why nothing was imported. The counts are 0 when set.</param>
/// <param name="Added">Values that did not exist before.</param>
/// <param name="Updated">Existing values that were changed.</param>
public record ImportResultDto(string? Error, int Added, int Updated);
