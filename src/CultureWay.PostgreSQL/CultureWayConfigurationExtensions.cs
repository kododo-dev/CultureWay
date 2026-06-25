using Kododo.CultureWay.Core.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Kododo.CultureWay.PostgreSQL;

/// <summary>
/// Extension methods for using PostgreSQL as the CultureWay persistence store.
/// </summary>
public static class CultureWayConfigurationExtensions
{
    /// <summary>
    /// Configures CultureWay to persist translations in a PostgreSQL database.
    /// The required schema and table are created automatically on first startup via
    /// <see cref="IStore.InitializeAsync"/>.
    /// </summary>
    /// <param name="configuration">The CultureWay configuration builder.</param>
    /// <param name="connectionString">
    /// A valid Npgsql connection string, e.g.
    /// <c>Host=localhost;Database=myapp;Username=postgres;Password=secret</c>.
    /// </param>
    public static ICultureWayConfiguration UsePostgreSQL(
        this ICultureWayConfiguration configuration,
        string connectionString)
    {
        configuration.Services.AddSingleton<IStore>(new Store(connectionString));
        return configuration;
    }

    /// <summary>
    /// Configures CultureWay to persist translations in a PostgreSQL database,
    /// resolving the connection string from the service provider at runtime.
    /// </summary>
    public static ICultureWayConfiguration UsePostgreSQL(
        this ICultureWayConfiguration configuration,
        Func<IServiceProvider, string> connectionStringFactory)
    {
        configuration.Services.AddSingleton<IStore>(
            sp => new Store(connectionStringFactory(sp)));
        return configuration;
    }
}
