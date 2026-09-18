using Kododo.CultureWay.Core.Model;

namespace Kododo.CultureWay.Core.Store;

/// <summary>
/// An optional read-only translation source (e.g. .resx resource files) that CultureWay can override.
/// Register implementations in DI to have them surfaced in the translation editor UI.
/// Keys from this source cannot be deleted — only overridden or reset to their default value.
/// </summary>
public interface IReadOnlySource
{
    /// <summary>
    /// Returns all baseline translations provided by this source.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetDefaultsAsync(CancellationToken cancellationToken = default);
}
