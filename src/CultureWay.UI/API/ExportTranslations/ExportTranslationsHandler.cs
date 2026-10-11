using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI.DTO;
using Kododo.CultureWay.UI.ImportExport;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.ExportTranslations;

internal sealed class ExportTranslationsHandler(
    IStore store,
    IEnumerable<IReadOnlySource> readOnlySources,
    CultureWayOptions options) : IRequestHandler<ExportTranslations, ExportFileDto>
{
    public async Task<ExportFileDto> HandleAsync(ExportTranslations request, CancellationToken cancellationToken)
    {
        var cultures = ResolveCultures(request.Cultures, out var unknown);
        var missingIn = request.MissingIn is null ? null : Resolve(request.MissingIn);
        if (request.MissingIn is not null && missingIn is null)
            unknown ??= request.MissingIn;
        if (unknown is not null)
            return ExportFileDto.Failed($"'{unknown}' is not one of the editor's languages.");

        var snapshot = await TranslationSnapshot.LoadAsync(store, readOnlySources, cancellationToken);
        var keys = snapshot.Keys
            .Where(k => TranslationSnapshot.InNamespace(k, request.Namespace))
            .Where(k => missingIn is null || snapshot.Get(k, missingIn) is null)
            .ToList();

        var suffix = FileNameSuffix(request.Namespace, missingIn);

        if (string.Equals(request.Format, "json", StringComparison.OrdinalIgnoreCase))
        {
            if (cultures.Count != 1)
                return ExportFileDto.Failed("A JSON export holds exactly one language.");

            var culture = cultures[0];
            // JSON has no empty values, so keys without a translation in this language are left out.
            var values = keys
                .Select(k => (Key: k, Value: snapshot.Get(k, culture)))
                .Where(x => x.Value is not null)
                .Select(x => new KeyValuePair<string, string>(x.Key, x.Value!));

            return new ExportFileDto(
                null,
                $"translations-{culture}{suffix}.json",
                TranslationFiles.JsonContentType,
                Convert.ToBase64String(TranslationFiles.WriteJson(values)));
        }

        if (string.Equals(request.Format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var rows = keys.Select(k => (k, (IReadOnlyDictionary<string, string>)cultures
                .Select(c => (Culture: c, Value: snapshot.Get(k, c)))
                .Where(x => x.Value is not null)
                .ToDictionary(x => x.Culture, x => x.Value!)));

            return new ExportFileDto(
                null,
                $"translations{suffix}.xlsx",
                TranslationFiles.XlsxContentType,
                Convert.ToBase64String(TranslationFiles.WriteXlsx(cultures, rows)));
        }

        return ExportFileDto.Failed($"Unknown export format '{request.Format}'. Use json or xlsx.");
    }

    private List<string> ResolveCultures(string[]? requested, out string? unknown)
    {
        unknown = null;
        if (requested is null || requested.Length == 0)
        {
            // The default language first, as the editor shows it.
            return options.SupportedCultures
                .OrderBy(c => string.Equals(c, options.DefaultCulture, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ToList();
        }

        var cultures = new List<string>();
        foreach (var code in requested)
        {
            var culture = Resolve(code);
            if (culture is null)
            {
                unknown = code;
                return [];
            }
            if (!cultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
                cultures.Add(culture);
        }
        return cultures;
    }

    private string? Resolve(string code) =>
        options.SupportedCultures.FirstOrDefault(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string FileNameSuffix(string? ns, string? missingIn)
    {
        var suffix = "";
        if (ns is { Length: > 0 })
            suffix += "-" + string.Concat(ns.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '_' ? ch : '_'));
        if (missingIn is not null)
            suffix += $"-missing-{missingIn}";
        return suffix;
    }
}
