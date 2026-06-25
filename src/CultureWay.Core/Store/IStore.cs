using Kododo.CultureWay.Core.Model;

namespace Kododo.CultureWay.Core.Store;

/// <summary>
/// Defines the persistence contract for CultureWay translations.
/// Implement this interface to use a custom storage backend (e.g. a database, a file, Redis).
/// </summary>
public interface IStore
{
    /// <summary>
    /// Called once at startup before the localizer is registered.
    /// Use this to run schema migrations, warm up connections, or perform any
    /// one-time initialisation required by the store.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all translations currently persisted in the store.
    /// </summary>
    Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the provided translations. Existing entries for the same
    /// (<see cref="Translation.Key"/>, <see cref="Translation.Culture"/>) pair are overwritten.
    /// </summary>
    Task SetAsync(IReadOnlyCollection<Translation> translations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes translations for the specified (key, culture) pairs.
    /// Non-existent pairs are silently ignored.
    /// </summary>
    Task DeleteAsync(IReadOnlyCollection<(string Key, string Culture)> keys, CancellationToken cancellationToken = default);
}
