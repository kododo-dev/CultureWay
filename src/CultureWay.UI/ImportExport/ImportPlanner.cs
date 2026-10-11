using System.Globalization;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.DTO;

namespace Kododo.CultureWay.UI.ImportExport;

/// <summary>
/// What a file asks to import. Preview and apply take the same input, so the server keeps no state
/// between the two: apply reads the file again and imports what the preview showed, as long as
/// nobody changed the translations in between.
/// </summary>
/// <param name="FileName">Decides the format by its extension: <c>.json</c> or <c>.xlsx</c>.</param>
/// <param name="ContentBase64">The file.</param>
/// <param name="Culture">The language of a JSON file. Excel files name theirs in the header row.</param>
/// <param name="OnlyMissing">Adds values that do not exist yet and leaves existing ones as they are.</param>
internal record ImportInput(string FileName, string ContentBase64, string? Culture, bool OnlyMissing);

internal sealed record ImportPlan(
    string? Error,
    IReadOnlyList<ImportChangeDto> Changes,
    int Unchanged,
    int Kept,
    IReadOnlyList<ImportProblemDto> Problems,
    IReadOnlyList<string> UnknownCultures)
{
    public static ImportPlan Failed(string error) => new(error, [], 0, 0, [], []);

    public ImportPreviewDto ToPreview() =>
        new(Error, [.. Changes], Unchanged, Kept, [.. Problems], [.. UnknownCultures]);

    /// <summary>The values to write, in the store's model.</summary>
    public IReadOnlyCollection<Translation> ToTranslations() =>
        Changes.Select(c => new Translation(c.Key, c.Culture, c.NewValue)).ToArray();
}

/// <summary>
/// Compares an import file with the current translations. Import never deletes: keys missing from
/// the file stay as they are.
/// </summary>
internal sealed class ImportPlanner(
    IStore store,
    IEnumerable<IReadOnlySource> readOnlySources,
    CultureWayOptions options,
    EditorOptions editorOptions)
{
    public async Task<ImportPlan> PlanAsync(ImportInput input, CancellationToken cancellationToken)
    {
        var content = Decode(input.ContentBase64, editorOptions.MaxImportBytes, out var error);
        if (content is null)
            return ImportPlan.Failed(error!);

        var unknownCultures = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        ReadResult read;
        switch (Path.GetExtension(input.FileName ?? "").ToLowerInvariant())
        {
            case ".json":
                if (string.IsNullOrWhiteSpace(input.Culture))
                    return ImportPlan.Failed("Choose the language of the JSON file.");
                var culture = ResolveCulture(input.Culture);
                if (culture is null)
                    return ImportPlan.Failed($"'{input.Culture}' is not one of the editor's languages.");
                read = TranslationFiles.ReadJson(content, culture);
                break;
            case ".xlsx":
                read = TranslationFiles.ReadXlsx(content, ResolveCulture, unknownCultures);
                break;
            default:
                return ImportPlan.Failed("Only .json and .xlsx files can be imported.");
        }

        if (read.Error is not null)
            return ImportPlan.Failed(read.Error);

        var snapshot = await TranslationSnapshot.LoadAsync(store, readOnlySources, cancellationToken);
        var problems = new List<ImportProblemDto>(read.Problems.Select(p => new ImportProblemDto(p.Location, p.Reason)));
        var changes = new List<ImportChangeDto>();
        var seen = new HashSet<(string, string)>();
        int unchanged = 0, kept = 0;

        foreach (var entry in read.Entries)
        {
            if (entry.Key != entry.Key.Trim())
            {
                problems.Add(new ImportProblemDto(entry.Location, $"The key \"{entry.Key}\" starts or ends with a space."));
                continue;
            }

            if (!seen.Add((entry.Key, entry.Culture.ToLowerInvariant())))
            {
                problems.Add(new ImportProblemDto(entry.Location, $"\"{entry.Key}\" ({entry.Culture}) appears more than once. The first one is used."));
                continue;
            }

            var current = snapshot.Get(entry.Key, entry.Culture);
            if (current == entry.Value)
                unchanged++;
            else if (current is not null && input.OnlyMissing)
                kept++;
            else
                changes.Add(new ImportChangeDto(entry.Key, entry.Culture, current, entry.Value));
        }

        return new ImportPlan(null, changes, unchanged, kept, problems, [.. unknownCultures]);
    }

    /// <summary>Matches a language code to one of the editor's, ignoring case (<c>pl-pl</c> is <c>pl-PL</c>).</summary>
    private string? ResolveCulture(string code)
    {
        var trimmed = code.Trim();
        return options.SupportedCultures.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static byte[]? Decode(string? base64, long maxBytes, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(base64))
        {
            error = "The file is empty.";
            return null;
        }

        // Checked before decoding, so an oversized upload is refused without allocating it twice.
        if (base64.Length / 4L * 3 > maxBytes)
        {
            error = $"The file is larger than {FormatSize(maxBytes)}.";
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length > maxBytes)
            {
                error = $"The file is larger than {FormatSize(maxBytes)}.";
                return null;
            }
            return bytes;
        }
        catch (FormatException)
        {
            error = "The file could not be read.";
            return null;
        }
    }

    private static string FormatSize(long bytes) => bytes >= 1024 * 1024
        ? (bytes / (1024d * 1024)).ToString("0.#", CultureInfo.InvariantCulture) + " MB"
        : (bytes / 1024d).ToString("0.#", CultureInfo.InvariantCulture) + " KB";
}
