using FluentAssertions;
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
        result.Should().ContainSingle(t => t.Key == "probe");
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_IsIdempotent()
    {
        var act = async () => await _store.InitializeAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAll_WhenTableIsEmpty_ReturnsEmptyList()
    {
        var result = await _store.GetAllAsync();
        result.Should().BeEmpty();
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

        result.Should().HaveCount(3);
        result.Select(t => t.Key).Should().BeEquivalentTo("Key1", "Key2", "Key3");
    }

    [Fact]
    public async Task GetAll_ReturnsCorrectCulturePerRow()
    {
        await _store.SetAsync([
            new Translation("Hello", "en", "Hello"),
            new Translation("Hello", "pl", "Cześć"),
        ]);

        var result = await _store.GetAllAsync();

        result.Should().Contain(t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello");
        result.Should().Contain(t => t.Key == "Hello" && t.Culture == "pl" && t.Value == "Cześć");
    }

    [Fact]
    public async Task Set_NewCompositeKey_InsertsRow()
    {
        await _store.SetAsync([new Translation("New", "en", "value")]);

        var result = await _store.GetAllAsync();
        result.Should().ContainSingle(t => t.Key == "New" && t.Culture == "en" && t.Value == "value");
    }

    [Fact]
    public async Task Set_ExistingCompositeKey_UpdatesValue()
    {
        await _store.SetAsync([new Translation("Key", "en", "OldValue")]);
        await _store.SetAsync([new Translation("Key", "en", "NewValue")]);

        var result = await _store.GetAllAsync();
        result.Should().ContainSingle();
        result.Single().Value.Should().Be("NewValue");
    }

    [Fact]
    public async Task Set_SameKeyDifferentCulture_InsertsBothRows()
    {
        await _store.SetAsync([
            new Translation("Key", "en", "English"),
            new Translation("Key", "pl", "Polski"),
        ]);

        var result = await _store.GetAllAsync();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Set_ExistingCompositeKey_DoesNotDuplicateRow()
    {
        await _store.SetAsync([new Translation("Key", "en", "A")]);
        await _store.SetAsync([new Translation("Key", "en", "B")]);
        await _store.SetAsync([new Translation("Key", "en", "C")]);

        var result = await _store.GetAllAsync();
        result.Should().ContainSingle(t => t.Key == "Key" && t.Culture == "en");
    }

    [Fact]
    public async Task Set_MultipleTranslations_AllInsertedInSingleCall()
    {
        var translations = Enumerable.Range(1, 10)
            .Select(i => new Translation($"Key{i}", "en", $"Value{i}"))
            .ToArray();

        await _store.SetAsync(translations);

        var result = await _store.GetAllAsync();
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Set_EmptyCollection_DoesNothing()
    {
        await _store.SetAsync([]);

        var result = await _store.GetAllAsync();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Set_FailedConnection_ThrowsException()
    {
        var brokenStore = new Store("Host=127.0.0.1;Port=9;Database=x;Username=x;Password=x");

        var act = async () => await brokenStore.SetAsync([new Translation("K", "en", "V")]);
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Set_DataPersistedAcrossStoreInstances()
    {
        await _store.SetAsync([new Translation("Persistent", "en", "data")]);

        var freshStore = new Store(_fixture.ConnectionString);
        await freshStore.InitializeAsync();

        var result = await freshStore.GetAllAsync();
        result.Should().Contain(t => t.Key == "Persistent" && t.Culture == "en" && t.Value == "data");
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
        result.Should().ContainSingle(t => t.Culture == "pl");
    }

    [Fact]
    public async Task Delete_NonExistingKey_SilentlyIgnored()
    {
        await _store.SetAsync([new Translation("Key", "en", "Value")]);

        var act = async () => await _store.DeleteAsync([("NonExisting", "en")]);
        await act.Should().NotThrowAsync();

        var result = await _store.GetAllAsync();
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_EmptyCollection_DoesNothing()
    {
        await _store.SetAsync([new Translation("Key", "en", "Value")]);

        await _store.DeleteAsync([]);

        var result = await _store.GetAllAsync();
        result.Should().ContainSingle();
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
        result.Should().ContainSingle(t => t.Key == "C");
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
        result.Should().BeEmpty();
    }
}

[CollectionDefinition("PostgreSQL")]
public class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlFixture> { }
