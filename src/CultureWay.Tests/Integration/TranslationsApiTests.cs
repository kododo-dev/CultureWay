using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Kododo.CultureWay.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Kododo.CultureWay.Tests.Integration;

public class TranslationsApiTests : IAsyncLifetime
{
    private WebApplication _app    = null!;
    private HttpClient     _client = null!;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl", "de"];
            x.Options.DefaultCulture    = "en";
            x.AddEditor();
        });

        _app = builder.Build();
        _app.UseCultureWay("/translations");

        await _app.InitializeCultureWayAsync();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // ── GetCultures ────────────────────────────────────────────────────

    [Fact]
    public async Task GetCultures_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetCultures", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCultures_ReturnsSupportedCultures()
    {
        var cultures = await FetchCultures();

        cultures.Should().HaveCount(3);
        cultures.Select(c => c.Code).Should().BeEquivalentTo("en", "pl", "de");
    }

    [Fact]
    public async Task GetCultures_DefaultCultureHasIsDefaultTrue()
    {
        var cultures = await FetchCultures();

        cultures.Single(c => c.Code == "en").IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetCultures_NonDefaultCulturesHaveIsDefaultFalse()
    {
        var cultures = await FetchCultures();

        cultures.Where(c => c.Code != "en")
                .Should().AllSatisfy(c => c.IsDefault.Should().BeFalse());
    }

    // ── AddCulture ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddCulture_ValidCode_Returns200WithEmptyErrors()
    {
        var response = await PostAddCulture("fr");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCulture_ValidCode_AppearsInGetCultures()
    {
        await PostAddCulture("fr");

        var cultures = await FetchCultures();

        cultures.Select(c => c.Code).Should().Contain("fr");
    }

    [Fact]
    public async Task AddCulture_AfterAdd_CanSaveTranslationInNewCulture()
    {
        await PostAddCulture("fr");

        var response = await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCulture_DuplicateCode_ReturnsValidationError()
    {
        var response = await PostAddCulture("en");

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task AddCulture_InvalidCode_ReturnsValidationError()
    {
        var response = await PostAddCulture("not-a-culture-!!");

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    // ── DeleteCulture ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCulture_NonDefaultCulture_Returns200WithEmptyErrors()
    {
        await PostAddCulture("fr");

        var response = await PostDeleteCulture("fr");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCulture_NonDefaultCulture_RemovedFromGetCultures()
    {
        await PostAddCulture("fr");

        await PostDeleteCulture("fr");

        var cultures = await FetchCultures();
        cultures.Select(c => c.Code).Should().NotContain("fr");
    }

    [Fact]
    public async Task DeleteCulture_WithExistingTranslations_TranslationsAreRemoved()
    {
        await PostAddCulture("fr");
        await PostUpdate(translations: [("Key", "fr", "Bonjour"), ("Key", "en", "Hello")]);

        await PostDeleteCulture("fr");

        var translations = await FetchTranslations();
        translations.Should().NotContain(t => t.Culture == "fr");
        translations.Should().Contain(t => t.Culture == "en");
    }

    [Fact]
    public async Task DeleteCulture_DefaultCulture_ReturnsValidationError()
    {
        var response = await PostDeleteCulture("en");

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();

        var cultures = await FetchCultures();
        cultures.Select(c => c.Code).Should().Contain("en");
    }

    [Fact]
    public async Task DeleteCulture_UnsupportedCulture_ReturnsValidationError()
    {
        var response = await PostDeleteCulture("fr");

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    // ── SetDefaultCulture ──────────────────────────────────────────────

    [Fact]
    public async Task SetDefaultCulture_SupportedCulture_Returns200WithEmptyErrors()
    {
        var response = await PostSetDefaultCulture("pl");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task SetDefaultCulture_SupportedCulture_UpdatesGetCultures()
    {
        await PostSetDefaultCulture("pl");

        var cultures = await FetchCultures();

        cultures.Single(c => c.Code == "pl").IsDefault.Should().BeTrue();
        cultures.Single(c => c.Code == "en").IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefaultCulture_UnsupportedCulture_ReturnsValidationError()
    {
        var response = await PostSetDefaultCulture("fr");

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    // ── GetTranslations ────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslations_InitiallyEmpty_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTranslations_InitiallyEmpty_ReturnsEmptyArray()
    {
        var translations = await FetchTranslations();
        translations.Should().BeEmpty();
    }

    // ── UpdateTranslations ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateTranslations_ValidData_Returns200WithEmptyErrors()
    {
        var response = await PostUpdate(
            translations: [("Hello", "en", "Hello World")]);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTranslations_AfterSave_GetReturnsUpdatedTranslation()
    {
        await PostUpdate(translations: [("Hello", "en", "Hello World")]);

        var translations = await FetchTranslations();

        translations.Should().Contain(t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello World");
    }

    [Fact]
    public async Task UpdateTranslations_MultipleLanguages_AllStored()
    {
        await PostUpdate(translations:
        [
            ("Hello", "en", "Hello"),
            ("Hello", "pl", "Cześć"),
            ("Hello", "de", "Hallo"),
        ]);

        var translations = await FetchTranslations();

        translations.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdateTranslations_OverwriteExisting_LastValueWins()
    {
        await PostUpdate(translations: [("Key", "en", "First")]);
        await PostUpdate(translations: [("Key", "en", "Second")]);

        var translations = await FetchTranslations();

        translations.Single(t => t.Key == "Key" && t.Culture == "en").Value.Should().Be("Second");
    }

    [Fact]
    public async Task UpdateTranslations_PartialUpdate_OtherTranslationsPreserved()
    {
        await PostUpdate(translations:
        [
            ("A", "en", "A-en"),
            ("B", "en", "B-en"),
        ]);

        await PostUpdate(translations: [("A", "en", "A-en-updated")]);

        var translations = await FetchTranslations();
        translations.Should().Contain(t => t.Key == "B" && t.Culture == "en" && t.Value == "B-en");
    }

    [Fact]
    public async Task UpdateTranslations_DeleteEntry_EntryNoLongerReturned()
    {
        await PostUpdate(translations:
        [
            ("Keep", "en", "keep"),
            ("Remove", "en", "remove"),
        ]);

        await PostUpdate(keysToDelete: [("Remove", "en")]);

        var translations = await FetchTranslations();
        translations.Should().NotContain(t => t.Key == "Remove");
        translations.Should().Contain(t => t.Key == "Keep");
    }

    [Fact]
    public async Task UpdateTranslations_DeleteEntryInOneCulture_OtherCulturePreserved()
    {
        await PostUpdate(translations:
        [
            ("Hello", "en", "Hello"),
            ("Hello", "pl", "Cześć"),
        ]);

        await PostUpdate(keysToDelete: [("Hello", "en")]);

        var translations = await FetchTranslations();
        translations.Should().NotContain(t => t.Key == "Hello" && t.Culture == "en");
        translations.Should().Contain(t => t.Key == "Hello" && t.Culture == "pl");
    }

    // ── Validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTranslations_UnsupportedCulture_ReturnsValidationError()
    {
        var response = await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle(e => e.Contains("fr") && e.Contains("SupportedCultures"));
    }

    [Fact]
    public async Task UpdateTranslations_UnsupportedCulture_TranslationNotStored()
    {
        await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        var translations = await FetchTranslations();
        translations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTranslations_EmptyKey_ReturnsValidationError()
    {
        var response = await PostUpdate(translations: [("", "en", "Value")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle(e => e.Contains("key") && e.Contains("empty"));
    }

    [Fact]
    public async Task UpdateTranslations_EmptyCulture_ReturnsValidationError()
    {
        var response = await PostUpdate(translations: [("Key", "", "Value")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateTranslations_MultipleErrors_AllReturned()
    {
        var response = await PostUpdate(translations:
        [
            ("",    "en", "A"),
            ("Key", "fr", "B"),
        ]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateTranslations_DeleteWithEmptyKey_ReturnsValidationError()
    {
        var response = await PostUpdate(keysToDelete: [("", "en")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateTranslations_DeleteWithEmptyCulture_ReturnsValidationError()
    {
        var response = await PostUpdate(keysToDelete: [("Key", "")]);

        var errors = await response.Content.ReadFromJsonAsync<string[]>(JsonOpts);
        errors.Should().ContainSingle();
    }

    // ── helpers ────────────────────────────────────────────────────────

    private async Task<TranslationResponse[]> FetchTranslations()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TranslationResponse[]>(JsonOpts))!;
    }

    private async Task<CultureResponse[]> FetchCultures()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetCultures", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CultureResponse[]>(JsonOpts))!;
    }

    private Task<HttpResponseMessage> PostAddCulture(string culture)
    {
        var json = JsonSerializer.Serialize(new { Culture = culture }, JsonOpts);
        return _client.PostAsync(
            "/translations/api/AddCulture",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private Task<HttpResponseMessage> PostDeleteCulture(string culture)
    {
        var json = JsonSerializer.Serialize(new { Culture = culture }, JsonOpts);
        return _client.PostAsync(
            "/translations/api/DeleteCulture",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private Task<HttpResponseMessage> PostSetDefaultCulture(string culture)
    {
        var json = JsonSerializer.Serialize(new { Culture = culture }, JsonOpts);
        return _client.PostAsync(
            "/translations/api/SetDefaultCulture",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private Task<HttpResponseMessage> PostUpdate(
        (string Key, string Culture, string Value)[] translations = default!,
        (string Key, string Culture)[] keysToDelete = default!)
    {
        var payload = new
        {
            Translations = (translations ?? [])
                .Select(t => new { t.Key, t.Culture, t.Value })
                .ToArray(),
            KeysToDelete = (keysToDelete ?? [])
                .Select(k => new { k.Key, k.Culture })
                .ToArray(),
        };
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        return _client.PostAsync(
            "/translations/api/UpdateTranslations",
            new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private sealed record TranslationResponse(string Key, string Culture, string Value);
    private sealed record CultureResponse(string Code, bool IsDefault);
}
