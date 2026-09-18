using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Npgsql;

namespace Kododo.CultureWay.PostgreSQL;

internal sealed class Store(string connectionString) : IStore
{
    private const string Schema = "cultureway";
    private const string Table = "translations";
    private const string CulturesTable = "cultures";
    private const string SettingsTable = "settings";
    private const string DefaultCultureSettingKey = "default_culture";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);
        await EnsureSchemaAsync(conn, cancellationToken);
        await EnsureTableAsync(conn, cancellationToken);
        await EnsureCulturesTableAsync(conn, cancellationToken);
        await EnsureSettingsTableAsync(conn, cancellationToken);
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

        // Last write wins per (key, culture), matching a sequential upsert loop — and avoids
        // Postgres' "ON CONFLICT DO UPDATE command cannot affect row a second time" error.
        var deduplicated = translations
            .GroupBy(t => (t.Key, t.Culture))
            .Select(g => g.Last())
            .ToArray();

        await using var conn = await OpenAsync(cancellationToken);

        var sql = $"""
                   INSERT INTO {Schema}.{Table} (key, culture, value)
                   SELECT * FROM UNNEST(@keys::text[], @cultures::text[], @values::text[])
                   ON CONFLICT (key, culture) DO UPDATE
                       SET value = EXCLUDED.value;
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("keys",     deduplicated.Select(t => t.Key).ToArray());
        cmd.Parameters.AddWithValue("cultures", deduplicated.Select(t => t.Culture).ToArray());
        cmd.Parameters.AddWithValue("values",   deduplicated.Select(t => t.Value).ToArray());
        await cmd.ExecuteNonQueryAsync(cancellationToken);
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

    public async Task<IReadOnlyList<string>> GetSupportedCulturesAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            $"SELECT code FROM {Schema}.{CulturesTable}", conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
            results.Add(reader.GetString(0));

        return results;
    }

    public async Task AddSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        var sql = $"""
                   INSERT INTO {Schema}.{CulturesTable} (code)
                   VALUES (@code)
                   ON CONFLICT (code) DO NOTHING;
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("code", culture);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RemoveSupportedCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            $"DELETE FROM {Schema}.{CulturesTable} WHERE code = @code", conn);
        cmd.Parameters.AddWithValue("code", culture);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetDefaultCultureAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(
            $"SELECT value FROM {Schema}.{SettingsTable} WHERE key = @key", conn);
        cmd.Parameters.AddWithValue("key", DefaultCultureSettingKey);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result as string;
    }

    public async Task SetDefaultCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        await using var conn = await OpenAsync(cancellationToken);

        var sql = $"""
                   INSERT INTO {Schema}.{SettingsTable} (key, value)
                   VALUES (@key, @value)
                   ON CONFLICT (key) DO UPDATE
                       SET value = EXCLUDED.value;
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("key", DefaultCultureSettingKey);
        cmd.Parameters.AddWithValue("value", culture);
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

    private static async Task EnsureCulturesTableAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = $"""
                   CREATE TABLE IF NOT EXISTS {Schema}.{CulturesTable} (
                       code TEXT NOT NULL,
                       CONSTRAINT pk_{CulturesTable} PRIMARY KEY (code)
                   );
                   """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnsureSettingsTableAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var sql = $"""
                   CREATE TABLE IF NOT EXISTS {Schema}.{SettingsTable} (
                       key   TEXT NOT NULL,
                       value TEXT NOT NULL,
                       CONSTRAINT pk_{SettingsTable} PRIMARY KEY (key)
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
