using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Kododo.CultureWay.PostgreSQL.Tests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithImage("postgres:16-alpine")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5432))
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM cultureway.translations", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
