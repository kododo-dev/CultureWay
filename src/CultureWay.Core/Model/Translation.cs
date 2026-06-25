namespace Kododo.CultureWay.Core.Model;

/// <summary>
/// A single localized string stored by CultureWay.
/// </summary>
/// <param name="Key">
/// The localization key, e.g. <c>Greeting</c> or <c>Validation.Required</c>.
/// </param>
/// <param name="Culture">
/// The BCP 47 culture code, e.g. <c>pl</c>, <c>en</c>, <c>en-US</c>.
/// </param>
/// <param name="Value">
/// The translated text for the given key and culture.
/// </param>
public record Translation(string Key, string Culture, string Value);
