using System.Globalization;
using System.Text.Json;
using MiniExcel = MiniExcelLibs.MiniExcel;
using ExcelType = MiniExcelLibs.ExcelType;

namespace Kododo.CultureWay.UI.ImportExport;

/// <summary>
/// One value read from an import file, with where it came from so a skipped row can be pointed at.
/// </summary>
internal sealed record FileEntry(string Key, string Culture, string Value, string Location);

/// <summary>
/// A row or property of an import file that could not be used.
/// </summary>
internal sealed record FileProblem(string Location, string Reason);

internal sealed record ReadResult(IReadOnlyList<FileEntry> Entries, IReadOnlyList<FileProblem> Problems, string? Error)
{
    public static ReadResult Failed(string error) => new([], [], error);
}

/// <summary>
/// Reads and writes the two import and export formats: a flat JSON object for one culture
/// (<c>{"Checkout.Pay": "Pay now"}</c>), and an Excel sheet with a <c>key</c> column followed by
/// one column per culture.
/// </summary>
/// <remarks>
/// JSON stays flat on purpose: a key such as <c>Checkout.Pay</c> can sit next to
/// <c>Checkout.Pay.Tooltip</c>, which a nested object cannot represent.
/// </remarks>
internal static class TranslationFiles
{
    public const string JsonContentType = "application/json";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string KeyHeader = "key";

    public static byte[] WriteJson(IEnumerable<KeyValuePair<string, string>> values)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = true,
            // Keeps non-Latin text readable in the file instead of \uXXXX escapes.
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            writer.WriteStartObject();
            foreach (var (key, value) in values)
                writer.WriteString(key, value);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static ReadResult ReadJson(byte[] content, string culture)
    {
        JsonDocument document;
        try
        {
            // Editors on Windows often save JSON with a byte order mark, which the parser rejects.
            ReadOnlyMemory<byte> json = content.AsSpan().StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])
                ? content.AsMemory(3)
                : content;
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException e)
        {
            return ReadResult.Failed($"The file is not valid JSON: {e.Message}");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return ReadResult.Failed("The file must contain a JSON object of keys and values.");

            var entries = new List<FileEntry>();
            var problems = new List<FileProblem>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var location = property.Name;
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        entries.Add(new FileEntry(property.Name, culture, property.Value.GetString()!, location));
                        break;
                    case JsonValueKind.Object:
                        problems.Add(new FileProblem(location, "Nested objects are not supported. Use flat keys such as \"Checkout.Pay\"."));
                        break;
                    case JsonValueKind.Null:
                        problems.Add(new FileProblem(location, "The value is empty."));
                        break;
                    default:
                        problems.Add(new FileProblem(location, "The value must be a string."));
                        break;
                }
            }

            return new ReadResult(entries, problems, null);
        }
    }

    public static byte[] WriteXlsx(IReadOnlyList<string> cultures, IEnumerable<(string Key, IReadOnlyDictionary<string, string> Values)> rows)
    {
        var sheet = rows.Select(row =>
        {
            var line = new Dictionary<string, object?>(StringComparer.Ordinal) { [KeyHeader] = row.Key };
            foreach (var culture in cultures)
                line[culture] = row.Values.TryGetValue(culture, out var value) ? value : null;
            return (IDictionary<string, object?>)line;
        }).ToList();

        // MiniExcel takes the header from the first row's keys, so an empty export still needs one.
        if (sheet.Count == 0)
        {
            var header = new Dictionary<string, object?>(StringComparer.Ordinal) { [KeyHeader] = null };
            foreach (var culture in cultures)
                header[culture] = null;
            sheet.Add(header);
        }

        using var stream = new MemoryStream();
        MiniExcel.SaveAs(stream, sheet, sheetName: "Translations", excelType: ExcelType.XLSX);
        return stream.ToArray();
    }

    /// <param name="content">The file.</param>
    /// <param name="resolveCulture">Maps a header to a culture code, or returns <c>null</c> for an unknown one.</param>
    /// <param name="unknownCultures">Headers that look like cultures but are not served.</param>
    public static ReadResult ReadXlsx(byte[] content, Func<string, string?> resolveCulture, ISet<string> unknownCultures)
    {
        List<IDictionary<string, object?>> rows;
        try
        {
            using var stream = new MemoryStream(content);
            rows = MiniExcel.Query(stream, useHeaderRow: false, excelType: ExcelType.XLSX)
                .Cast<IDictionary<string, object?>>()
                .ToList();
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            return ReadResult.Failed("The file is not a readable Excel workbook (.xlsx).");
        }

        if (rows.Count == 0)
            return ReadResult.Failed("The sheet is empty.");

        // Columns are named A, B, C... by MiniExcel; the first row says what each one holds.
        var header = rows[0];
        string? keyColumn = null;
        var cultureColumns = new List<(string Column, string Culture)>();
        foreach (var (column, cell) in header)
        {
            var title = CellText(cell)?.Trim();
            if (string.IsNullOrEmpty(title))
                continue;

            if (keyColumn is null && string.Equals(title, KeyHeader, StringComparison.OrdinalIgnoreCase))
            {
                keyColumn = column;
                continue;
            }

            var culture = resolveCulture(title);
            if (culture is null)
                unknownCultures.Add(title);
            else
                cultureColumns.Add((column, culture));
        }

        if (keyColumn is null)
            return ReadResult.Failed("The first row must have a \"key\" column followed by one column per language, e.g. key | en | pl.");

        var entries = new List<FileEntry>();
        var problems = new List<FileProblem>();
        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var location = $"Row {i + 1}";
            var key = row.TryGetValue(keyColumn, out var keyCell) ? CellText(keyCell) : null;
            var hasValues = cultureColumns.Any(c => !string.IsNullOrEmpty(CellText(row.TryGetValue(c.Column, out var v) ? v : null)));

            if (string.IsNullOrWhiteSpace(key))
            {
                if (hasValues)
                    problems.Add(new FileProblem(location, "The key is empty."));
                continue;
            }

            foreach (var (column, culture) in cultureColumns)
            {
                var value = CellText(row.TryGetValue(column, out var cell) ? cell : null);
                if (!string.IsNullOrEmpty(value))
                    entries.Add(new FileEntry(key, culture, value, location));
            }
        }

        return new ReadResult(entries, problems, null);
    }

    private static string? CellText(object? cell) => cell switch
    {
        null => null,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => cell.ToString(),
    };
}
