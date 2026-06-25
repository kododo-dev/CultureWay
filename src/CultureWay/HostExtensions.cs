using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kododo.CultureWay;

public static class HostExtensions
{
    /// <summary>
    /// Initializes the CultureWay store (runs schema migrations if needed) and
    /// loads all translations into the in-memory cache. Call this after <see cref="IHostBuilder.Build"/>.
    /// </summary>
    public static async Task InitializeCultureWayAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IStore>();
        await store.InitializeAsync(cancellationToken);

        var translations = await store.GetAllAsync(cancellationToken);
        var cache = host.Services.GetRequiredService<TranslationCache>();
        cache.Load(translations);
    }
}
