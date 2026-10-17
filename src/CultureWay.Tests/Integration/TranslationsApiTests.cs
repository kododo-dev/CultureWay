using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        _app.UseCultureWay("/translations").AllowAnonymous();

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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCultures_ReturnsSupportedCultures()
    {
        var cultures = await FetchCultures();

        Assert.Equal(3, cultures.Count());
        Assert.Equivalent(new[] { "en", "pl", "de" }, cultures.Select(c => c.Code), strict: true);
    }

    [Fact]
    public async Task GetCultures_DefaultCultureHasIsDefaultTrue()
    {
        var cultures = await FetchCultures();

        Assert.True(cultures.Single(c => c.Code == "en").IsDefault);
    }

    [Fact]
    public async Task GetCultures_NonDefaultCulturesHaveIsDefaultFalse()
    {
        var cultures = await FetchCultures();

        Assert.All(cultures.Where(c => c.Code != "en"), c => Assert.False(c.IsDefault));
    }

    // ── AddCulture ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddCulture_ValidCode_Returns204()
    {
        var response = await PostAddCulture("fr");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddCulture_ValidCode_AppearsInGetCultures()
    {
        await PostAddCulture("fr");

        var cultures = await FetchCultures();

        Assert.Contains("fr", cultures.Select(c => c.Code));
    }

    [Fact]
    public async Task AddCulture_AfterAdd_CanSaveTranslationInNewCulture()
    {
        await PostAddCulture("fr");

        var response = await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddCulture_DuplicateCode_Returns409()
    {
        var response = await PostAddCulture("en");

        var errors = await ReadErrorsAsync(response, HttpStatusCode.Conflict);
        Assert.Single(errors!);
    }

    [Fact]
    public async Task AddCulture_InvalidCode_Returns400()
    {
        var response = await PostAddCulture("not-a-culture-!!");

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!);
    }

    // ── DeleteCulture ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCulture_NonDefaultCulture_Returns204()
    {
        await PostAddCulture("fr");

        var response = await PostDeleteCulture("fr");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCulture_NonDefaultCulture_RemovedFromGetCultures()
    {
        await PostAddCulture("fr");

        await PostDeleteCulture("fr");

        var cultures = await FetchCultures();
        Assert.DoesNotContain("fr", cultures.Select(c => c.Code));
    }

    [Fact]
    public async Task DeleteCulture_WithExistingTranslations_TranslationsAreRemoved()
    {
        await PostAddCulture("fr");
        await PostUpdate(translations: [("Key", "fr", "Bonjour"), ("Key", "en", "Hello")]);

        await PostDeleteCulture("fr");

        var translations = await FetchTranslations();
        Assert.DoesNotContain(translations, t => t.Culture == "fr");
        Assert.Contains(translations, t => t.Culture == "en");
    }

    [Fact]
    public async Task DeleteCulture_DefaultCulture_Returns409()
    {
        var response = await PostDeleteCulture("en");

        var errors = await ReadErrorsAsync(response, HttpStatusCode.Conflict);
        Assert.Single(errors!);

        var cultures = await FetchCultures();
        Assert.Contains("en", cultures.Select(c => c.Code));
    }

    [Fact]
    public async Task DeleteCulture_UnsupportedCulture_Returns404()
    {
        var response = await PostDeleteCulture("fr");

        var errors = await ReadErrorsAsync(response, HttpStatusCode.NotFound);
        Assert.Single(errors!);
    }

    // ── SetDefaultCulture ──────────────────────────────────────────────

    [Fact]
    public async Task SetDefaultCulture_SupportedCulture_Returns204()
    {
        var response = await PostSetDefaultCulture("pl");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetDefaultCulture_SupportedCulture_UpdatesGetCultures()
    {
        await PostSetDefaultCulture("pl");

        var cultures = await FetchCultures();

        Assert.True(cultures.Single(c => c.Code == "pl").IsDefault);
        Assert.False(cultures.Single(c => c.Code == "en").IsDefault);
    }

    [Fact]
    public async Task SetDefaultCulture_UnsupportedCulture_Returns404()
    {
        var response = await PostSetDefaultCulture("fr");

        var errors = await ReadErrorsAsync(response, HttpStatusCode.NotFound);
        Assert.Single(errors!);
    }

    // ── GetTranslations ────────────────────────────────────────────────

    [Fact]
    public async Task GetTranslations_InitiallyEmpty_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTranslations_InitiallyEmpty_ReturnsEmptyArray()
    {
        var translations = await FetchTranslations();
        Assert.Empty(translations);
    }

    // ── UpdateTranslations ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateTranslations_ValidData_Returns204()
    {
        var response = await PostUpdate(
            translations: [("Hello", "en", "Hello World")]);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTranslations_AfterSave_GetReturnsUpdatedTranslation()
    {
        await PostUpdate(translations: [("Hello", "en", "Hello World")]);

        var translations = await FetchTranslations();

        Assert.Contains(translations, t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello World");
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

        Assert.Equal(3, translations.Count());
    }

    [Fact]
    public async Task UpdateTranslations_OverwriteExisting_LastValueWins()
    {
        await PostUpdate(translations: [("Key", "en", "First")]);
        await PostUpdate(translations: [("Key", "en", "Second")]);

        var translations = await FetchTranslations();

        Assert.Equal("Second", translations.Single(t => t.Key == "Key" && t.Culture == "en").Value);
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
        Assert.Contains(translations, t => t.Key == "B" && t.Culture == "en" && t.Value == "B-en");
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
        Assert.DoesNotContain(translations, t => t.Key == "Remove");
        Assert.Contains(translations, t => t.Key == "Keep");
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
        Assert.DoesNotContain(translations, t => t.Key == "Hello" && t.Culture == "en");
        Assert.Contains(translations, t => t.Key == "Hello" && t.Culture == "pl");
    }

    // ── Validation ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTranslations_UnsupportedCulture_Returns400()
    {
        var response = await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!, e => e.Contains("fr") && e.Contains("SupportedCultures"));
    }

    [Fact]
    public async Task UpdateTranslations_UnsupportedCulture_TranslationNotStored()
    {
        await PostUpdate(translations: [("Key", "fr", "Bonjour")]);

        var translations = await FetchTranslations();
        Assert.Empty(translations);
    }

    [Fact]
    public async Task UpdateTranslations_EmptyKey_Returns400()
    {
        var response = await PostUpdate(translations: [("", "en", "Value")]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!, e => e.Contains("key") && e.Contains("empty"));
    }

    [Fact]
    public async Task UpdateTranslations_EmptyCulture_Returns400()
    {
        var response = await PostUpdate(translations: [("Key", "", "Value")]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!);
    }

    [Fact]
    public async Task UpdateTranslations_MultipleErrors_AllReturned()
    {
        var response = await PostUpdate(translations:
        [
            ("",    "en", "A"),
            ("Key", "fr", "B"),
        ]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(2, errors!.Count());
    }

    [Fact]
    public async Task UpdateTranslations_DeleteWithEmptyKey_Returns400()
    {
        var response = await PostUpdate(keysToDelete: [("", "en")]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!);
    }

    [Fact]
    public async Task UpdateTranslations_DeleteWithEmptyCulture_Returns400()
    {
        var response = await PostUpdate(keysToDelete: [("Key", "")]);

        var errors = await ReadErrorsAsync(response, HttpStatusCode.BadRequest);
        Assert.Single(errors!);
    }

    // ── helpers ────────────────────────────────────────────────────────

    /// <summary>The messages of a problem details response: its field errors, or else its detail.</summary>
    private static async Task<string[]> ReadErrorsAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return problem.TryGetProperty("errors", out var errors)
            ? errors.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString()!)).ToArray()
            : [problem.GetProperty("detail").GetString()!];
    }

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
