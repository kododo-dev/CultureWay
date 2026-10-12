using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.PostgreSQL.Tests.Fixtures;
using Xunit;

namespace Kododo.CultureWay.PostgreSQL.Tests;

[Collection("PostgreSQL")]
public class StoreTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private Store _store = null!;

    public StoreTests(PostgreSqlFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _store = new Store(_fixture.ConnectionString);
        await _store.InitializeAsync();
        await _fixture.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InitializeAsync_CreatesSchemaAndTable()
    {
        await _store.SetAsync([new Translation("probe", "en", "ok")]);
        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "probe");
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_IsIdempotent()
    {
        await _store.InitializeAsync();
    }

    [Fact]
    public async Task GetAll_WhenTableIsEmpty_ReturnsEmptyList()
    {
        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllStoredRows()
    {
        await _store.SetAsync([
            new Translation("Key1", "en", "Value1"),
            new Translation("Key2", "en", "Value2"),
            new Translation("Key3", "pl", "Value3"),
        ]);

        var result = await _store.GetAllAsync();

        Assert.Equal(3, result.Count());
        Assert.Equivalent(new[] { "Key1", "Key2", "Key3" }, result.Select(t => t.Key), strict: true);
    }

    [Fact]
    public async Task GetAll_ReturnsCorrectCulturePerRow()
    {
        await _store.SetAsync([
            new Translation("Hello", "en", "Hello"),
            new Translation("Hello", "pl", "Cześć"),
        ]);

        var result = await _store.GetAllAsync();

        Assert.Contains(result, t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello");
        Assert.Contains(result, t => t.Key == "Hello" && t.Culture == "pl" && t.Value == "Cześć");
    }

    [Fact]
    public async Task Set_NewCompositeKey_InsertsRow()
    {
        await _store.SetAsync([new Translation("New", "en", "value")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "New" && t.Culture == "en" && t.Value == "value");
    }

    [Fact]
    public async Task Set_ExistingCompositeKey_UpdatesValue()
    {
        await _store.SetAsync([new Translation("Key", "en", "OldValue")]);
        await _store.SetAsync([new Translation("Key", "en", "NewValue")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result);
        Assert.Equal("NewValue", result.Single().Value);
    }

    [Fact]
    public async Task Set_SameKeyDifferentCulture_InsertsBothRows()
    {
        await _store.SetAsync([
            new Translation("Key", "en", "English"),
            new Translation("Key", "pl", "Polski"),
        ]);

        var result = await _store.GetAllAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task Set_ExistingCompositeKey_DoesNotDuplicateRow()
    {
        await _store.SetAsync([new Translation("Key", "en", "A")]);
        await _store.SetAsync([new Translation("Key", "en", "B")]);
        await _store.SetAsync([new Translation("Key", "en", "C")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "Key" && t.Culture == "en");
    }

    [Fact]
    public async Task Set_MultipleTranslations_AllInsertedInSingleCall()
    {
        var translations = Enumerable.Range(1, 10)
            .Select(i => new Translation($"Key{i}", "en", $"Value{i}"))
            .ToArray();

        await _store.SetAsync(translations);

        var result = await _store.GetAllAsync();
        Assert.Equal(10, result.Count());
    }

    [Fact]
    public async Task Set_EmptyCollection_DoesNothing()
    {
        await _store.SetAsync([]);

        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task Set_FailedConnection_ThrowsException()
    {
        var brokenStore = new Store("Host=127.0.0.1;Port=9;Database=x;Username=x;Password=x");

        await Assert.ThrowsAnyAsync<Exception>(() => brokenStore.SetAsync([new Translation("K", "en", "V")]));
    }

    [Fact]
    public async Task Set_DataPersistedAcrossStoreInstances()
    {
        await _store.SetAsync([new Translation("Persistent", "en", "data")]);

        var freshStore = new Store(_fixture.ConnectionString);
        await freshStore.InitializeAsync();

        var result = await freshStore.GetAllAsync();
        Assert.Contains(result, t => t.Key == "Persistent" && t.Culture == "en" && t.Value == "data");
    }

    [Fact]
    public async Task Delete_ExistingCompositeKey_RemovesRow()
    {
        await _store.SetAsync([
            new Translation("Hello", "en", "Hello"),
            new Translation("Hello", "pl", "Cześć"),
        ]);

        await _store.DeleteAsync([("Hello", "en")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Culture == "pl");
    }

    [Fact]
    public async Task Delete_NonExistingKey_SilentlyIgnored()
    {
        await _store.SetAsync([new Translation("Key", "en", "Value")]);

        await _store.DeleteAsync([("NonExisting", "en")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task Delete_EmptyCollection_DoesNothing()
    {
        await _store.SetAsync([new Translation("Key", "en", "Value")]);

        await _store.DeleteAsync([]);

        var result = await _store.GetAllAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task Delete_BulkDelete_AllMatchingRemoved()
    {
        await _store.SetAsync([
            new Translation("A", "en", "1"),
            new Translation("B", "en", "2"),
            new Translation("C", "en", "3"),
        ]);

        await _store.DeleteAsync([("A", "en"), ("B", "en")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "C");
    }

    [Fact]
    public async Task Delete_AllRows_TableIsEmpty()
    {
        await _store.SetAsync([
            new Translation("A", "en", "1"),
            new Translation("A", "pl", "2"),
        ]);

        await _store.DeleteAsync([("A", "en"), ("A", "pl")]);

        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task InitializeAsync_CreatesCulturesTable()
    {
        await _store.AddSupportedCultureAsync("fr");
        var result = await _store.GetSupportedCulturesAsync();
        Assert.Single(result, c => c == "fr");
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_CulturesTableIsIdempotent()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.InitializeAsync();

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Single(result, c => c == "fr");
    }

    [Fact]
    public async Task GetSupportedCultures_WhenTableIsEmpty_ReturnsEmptyList()
    {
        var result = await _store.GetSupportedCulturesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddSupportedCulture_AddedTwice_DoesNotDuplicate()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.AddSupportedCultureAsync("fr");

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task AddSupportedCulture_DataPersistedAcrossStoreInstances()
    {
        await _store.AddSupportedCultureAsync("fr");

        var freshStore = new Store(_fixture.ConnectionString);
        await freshStore.InitializeAsync();

        var result = await freshStore.GetSupportedCulturesAsync();
        Assert.Contains("fr", result);
    }

    [Fact]
    public async Task RemoveSupportedCulture_ExistingCulture_RemovesIt()
    {
        await _store.AddSupportedCultureAsync("fr");

        await _store.RemoveSupportedCultureAsync("fr");

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task RemoveSupportedCulture_NonExistingCulture_SilentlyIgnored()
    {
        await _store.AddSupportedCultureAsync("fr");

        await _store.RemoveSupportedCultureAsync("it");

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task RemoveSupportedCulture_RemovalPersistedAcrossStoreInstances()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.RemoveSupportedCultureAsync("fr");

        var freshStore = new Store(_fixture.ConnectionString);
        await freshStore.InitializeAsync();

        var result = await freshStore.GetSupportedCulturesAsync();
        Assert.DoesNotContain("fr", result);
    }

    [Fact]
    public async Task InitializeAsync_CreatesSettingsTable()
    {
        await _store.SetDefaultCultureAsync("pl");
        var result = await _store.GetDefaultCultureAsync();
        Assert.Equal("pl", result);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_SettingsTableIsIdempotent()
    {
        await _store.SetDefaultCultureAsync("pl");
        await _store.InitializeAsync();

        var result = await _store.GetDefaultCultureAsync();
        Assert.Equal("pl", result);
    }

    [Fact]
    public async Task GetDefaultCulture_WhenNotSet_ReturnsNull()
    {
        var result = await _store.GetDefaultCultureAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SetDefaultCulture_CalledAgain_OverwritesPreviousValue()
    {
        await _store.SetDefaultCultureAsync("pl");
        await _store.SetDefaultCultureAsync("de");

        var result = await _store.GetDefaultCultureAsync();
        Assert.Equal("de", result);
    }

    [Fact]
    public async Task SetDefaultCulture_DataPersistedAcrossStoreInstances()
    {
        await _store.SetDefaultCultureAsync("pl");

        var freshStore = new Store(_fixture.ConnectionString);
        await freshStore.InitializeAsync();

        var result = await freshStore.GetDefaultCultureAsync();
        Assert.Equal("pl", result);
    }
}

[CollectionDefinition("PostgreSQL")]
public class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlFixture> { }
