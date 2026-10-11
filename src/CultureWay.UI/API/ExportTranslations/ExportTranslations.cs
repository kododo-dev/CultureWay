using Kododo.CultureWay.UI.DTO;
using Kododo.Reiho.AspNetCore.API;

namespace Kododo.CultureWay.UI.API.ExportTranslations;

/// <summary>Exports the translations the editor shows.</summary>
/// <param name="Format"><c>json</c> (one language, flat keys) or <c>xlsx</c> (a column per language).</param>
/// <param name="Cultures">
/// The languages to export, in column order. JSON takes exactly one. Every language of the editor when empty.
/// </param>
/// <param name="Namespace">
/// Limits the export to a namespace and the ones nested in it, as the editor's side menu does.
/// <c>null</c> exports every key, an empty string the keys without a namespace.
/// </param>
/// <param name="MissingIn">Only keys that have no value in this language, for handing work to a translator.</param>
internal record ExportTranslations(string Format, string[]? Cultures, string? Namespace, string? MissingIn)
    : IRequest<ExportFileDto>;
