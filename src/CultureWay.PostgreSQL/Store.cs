using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Npgsql;
using NpgsqlTypes;

namespace Kododo.CultureWay.PostgreSQL;

internal sealed class Store(string connectionString) : IStore
{
    private const string Schema = "cultureway";
    private const string Table = "translations";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(conn, cancellationToken);
        await EnsureTableAsync(conn, cancellationToken);
    }

    public async Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            $"SELECT key, culture, value FROM {Schema}.{Table}", conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<Translation>();

        while (await reader.ReadAsync(cancellationToken))
            results.Add(new Translation(reader.GetString(0), reader.GetString(1), reader.GetString(2)));

        return results;
    }

    public async Task SetAsync(IReadOnlyCollection<Translation> translations, CancellationToken cancellationToken = default)
    {
        if (translations.Count == 0)
            return;

        await using var conn = await OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            var sql = $"""
                       INSERT INTO {Schema}.{Table} (key, culture, value)
                       VALUES (@key, @culture, @value)
                       ON CONFLICT (key, culture) DO UPDATE
                           SET value = EXCLUDED.value;
                       """;

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            var keyParam     = cmd.Parameters.Add(new NpgsqlParameter("key",     NpgsqlDbType.Text));
            var cultureParam = cmd.Parameters.Add(new NpgsqlParameter("culture", NpgsqlDbType.Text));
            var valueParam   = cmd.Parameters.Add(new NpgsqlParameter("value",   NpgsqlDbType.Text));
            await cmd.PrepareAsync(cancellationToken);

            foreach (var t in translations)
            {
                keyParam.Value     = t.Key;
                cultureParam.Value = t.Culture;
                valueParam.Value   = t.Value;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(IReadOnlyCollection<(string Key, string Culture)> keys, CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return;

        await using var conn = await OpenAsync(cancellationToken);

        var sql = $"""
                   DELETE FROM {Schema}.{Table}
                   WHERE (key, culture) IN (
                       SELECT UNNEST(@keys::text[]), UNNEST(@cultures::text[])
                   )
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("keys",     keys.Select(k => k.Key).ToArray());
        cmd.Parameters.AddWithValue("cultures", keys.Select(k => k.Culture).ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS \"{Schema}\"", conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureTableAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = $"""
                   CREATE TABLE IF NOT EXISTS {Schema}.{Table} (
                       key        TEXT NOT NULL,
                       culture    TEXT NOT NULL,
                       value      TEXT NOT NULL,
                       CONSTRAINT pk_{Table} PRIMARY KEY (key, culture)
                   );
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
