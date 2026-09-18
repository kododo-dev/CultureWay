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

    /// <summary>
    /// Returns culture codes that were added at runtime through the translation editor,
    /// in addition to the cultures configured via <c>CultureWayOptions.SupportedCultures</c>.
    /// The default implementation returns an empty list — override to persist runtime-added cultures
    /// so they survive an application restart.
    /// </summary>
    Task<IReadOnlyList<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    /// <summary>
    /// Persists a culture code added at runtime through the translation editor so it can be
    /// restored by <see cref="GetSupportedCulturesAsync"/> after a restart.
    /// The default implementation is a no-op.
    /// </summary>
    Task AddSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Removes a culture code previously persisted by <see cref="AddSupportedCultureAsync"/>,
    /// so it is no longer returned by <see cref="GetSupportedCulturesAsync"/>.
    /// Non-existent codes are silently ignored. The default implementation is a no-op.
    /// </summary>
    Task RemoveSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Returns the default culture code persisted at runtime through the translation editor,
    /// or <c>null</c> if none has been set, in which case <c>CultureWayOptions.DefaultCulture</c>
    /// from startup configuration applies. The default implementation returns <c>null</c>.
    /// </summary>
    Task<string?> GetDefaultCultureAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    /// <summary>
    /// Persists the default culture code selected at runtime through the translation editor so it
    /// can be restored by <see cref="GetDefaultCultureAsync"/> after a restart.
    /// The default implementation is a no-op.
    /// </summary>
    Task SetDefaultCultureAsync(string culture, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
