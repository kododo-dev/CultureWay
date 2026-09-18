using System.Collections.Concurrent;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;

namespace Kododo.CultureWay.Store;

internal sealed class InMemoryStore : IStore
{
    private readonly ConcurrentDictionary<(string Key, string Culture), Translation> _store = new();
    private readonly ConcurrentDictionary<string, byte> _cultures = new(StringComparer.OrdinalIgnoreCase);
    private string? _defaultCulture;

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Translation>>(_store.Values.ToList());

    public Task SetAsync(IReadOnlyCollection<Translation> translations, CancellationToken cancellationToken = default)
    {
        foreach (var t in translations)
            _store[(t.Key, t.Culture)] = t;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(IReadOnlyCollection<(string Key, string Culture)> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
            _store.TryRemove(key, out _);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(_cultures.Keys.ToList());

    public Task AddSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        _cultures[culture] = 0;
        return Task.CompletedTask;
    }

    public Task RemoveSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        _cultures.TryRemove(culture, out _);
        return Task.CompletedTask;
    }

    public Task<string?> GetDefaultCultureAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_defaultCulture);

    public Task SetDefaultCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        _defaultCulture = culture;
        return Task.CompletedTask;
    }
}
