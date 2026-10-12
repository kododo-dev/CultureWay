using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Store;
using Xunit;

namespace Kododo.CultureWay.Tests.Unit;

public class InMemoryStoreTests
{
    private readonly InMemoryStore _store = new();

    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAll_AfterSet_ReturnsStoredTranslations()
    {
        await _store.SetAsync([
            new Translation("Hello", "en", "Hello"),
            new Translation("Hello", "pl", "Cześć"),
        ]);

        var result = await _store.GetAllAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello");
        Assert.Contains(result, t => t.Key == "Hello" && t.Culture == "pl" && t.Value == "Cześć");
    }

    [Fact]
    public async Task Set_ExistingCompositeKey_OverwritesValue()
    {
        await _store.SetAsync([new Translation("Key", "en", "OldValue")]);
        await _store.SetAsync([new Translation("Key", "en", "NewValue")]);

        var result = await _store.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("NewValue", result.Single().Value);
    }

    [Fact]
    public async Task Set_SameKeyDifferentCultures_BothStored()
    {
        await _store.SetAsync([
            new Translation("Key", "en", "English"),
            new Translation("Key", "pl", "Polski"),
            new Translation("Key", "de", "Deutsch"),
        ]);

        var result = await _store.GetAllAsync();
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task Set_SameKeyAndCultureMultipleTimes_NoDuplicates()
    {
        await _store.SetAsync([new Translation("Key", "en", "A")]);
        await _store.SetAsync([new Translation("Key", "en", "B")]);
        await _store.SetAsync([new Translation("Key", "en", "C")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "Key" && t.Culture == "en");
    }

    [Fact]
    public async Task Set_MultipleTranslations_AllInserted()
    {
        var translations = Enumerable.Range(1, 5)
            .Select(i => new Translation($"Key{i}", "en", $"Value{i}"))
            .ToArray();

        await _store.SetAsync(translations);

        var result = await _store.GetAllAsync();
        Assert.Equal(5, result.Count());
    }

    [Fact]
    public async Task Set_EmptyCollection_DoesNothing()
    {
        await _store.SetAsync([]);

        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task Delete_ExistingCompositeKey_RemovesEntry()
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
    public async Task Delete_AllKeys_StoreIsEmpty()
    {
        await _store.SetAsync([
            new Translation("A", "en", "1"),
            new Translation("B", "en", "2"),
        ]);

        await _store.DeleteAsync([("A", "en"), ("B", "en")]);

        var result = await _store.GetAllAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task Delete_OneOfSeveralKeys_OthersRemain()
    {
        await _store.SetAsync([
            new Translation("Keep", "en", "keep"),
            new Translation("Remove", "en", "remove"),
        ]);

        await _store.DeleteAsync([("Remove", "en")]);

        var result = await _store.GetAllAsync();
        Assert.Single(result, t => t.Key == "Keep");
    }

    [Fact]
    public async Task InitializeAsync_AlwaysCompletes()
    {
        await _store.InitializeAsync();
    }

    [Fact]
    public async Task GetSupportedCultures_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _store.GetSupportedCulturesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSupportedCultures_AfterAdd_ReturnsAddedCulture()
    {
        await _store.AddSupportedCultureAsync("fr");

        var result = await _store.GetSupportedCulturesAsync();

        Assert.Equal("fr", Assert.Single(result));
    }

    [Fact]
    public async Task AddSupportedCulture_AddedTwice_NoDuplicates()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.AddSupportedCultureAsync("fr");

        var result = await _store.GetSupportedCulturesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task AddSupportedCulture_DifferentCasing_TreatedAsDuplicate()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.AddSupportedCultureAsync("FR");

        var result = await _store.GetSupportedCulturesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task AddSupportedCulture_MultipleDifferentCultures_AllStored()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.AddSupportedCultureAsync("it");
        await _store.AddSupportedCultureAsync("es");

        var result = await _store.GetSupportedCulturesAsync();

        Assert.Equivalent(new[] { "fr", "it", "es" }, result, strict: true);
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
    public async Task RemoveSupportedCulture_DifferentCasing_StillRemoves()
    {
        await _store.AddSupportedCultureAsync("fr");

        await _store.RemoveSupportedCultureAsync("FR");

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task RemoveSupportedCulture_OneOfSeveral_OthersRemain()
    {
        await _store.AddSupportedCultureAsync("fr");
        await _store.AddSupportedCultureAsync("it");

        await _store.RemoveSupportedCultureAsync("fr");

        var result = await _store.GetSupportedCulturesAsync();
        Assert.Equal("it", Assert.Single(result));
    }

    [Fact]
    public async Task GetDefaultCulture_WhenNotSet_ReturnsNull()
    {
        var result = await _store.GetDefaultCultureAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDefaultCulture_AfterSet_ReturnsSetCulture()
    {
        await _store.SetDefaultCultureAsync("pl");

        var result = await _store.GetDefaultCultureAsync();
        Assert.Equal("pl", result);
    }

    [Fact]
    public async Task SetDefaultCulture_CalledAgain_OverwritesPreviousValue()
    {
        await _store.SetDefaultCultureAsync("pl");
        await _store.SetDefaultCultureAsync("de");

        var result = await _store.GetDefaultCultureAsync();
        Assert.Equal("de", result);
    }
}
