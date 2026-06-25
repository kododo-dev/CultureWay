using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Kododo.CultureWay.PostgreSQL.Tests.Fixtures;
using Kododo.CultureWay.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Kododo.CultureWay.PostgreSQL.Tests.Integration;

[Collection("PostgreSQL")]
public class PostgresApiTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private WebApplication _app    = null!;
    private HttpClient     _client = null!;

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public PostgresApiTests(PostgreSqlFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _app    = await BuildAppAsync();
        _client = _app.GetTestClient();
        await _fixture.ResetAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetTranslations_Returns200()
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

    [Fact]
    public async Task GetCultures_ReturnsSupportedCultures()
    {
        var cultures = await FetchCultures();

        cultures.Should().HaveCount(3);
        cultures.Select(c => c.Code).Should().BeEquivalentTo("en", "pl", "de");
    }

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
    public async Task UpdateTranslations_AfterSave_GetReturnsUpdatedValue()
    {
        await PostUpdate(translations: [("Hello", "en", "Hello World")]);

        var translations = await FetchTranslations();

        translations.Should().Contain(t => t.Key == "Hello" && t.Culture == "en" && t.Value == "Hello World");
    }

    [Fact]
    public async Task UpdateTranslations_PersistedToPostgres_SurvivesAppRestart()
    {
        await PostUpdate(translations: [("Persistent", "en", "survived")]);

        await using var restartedApp = await BuildAppAsync();
        using var restartedClient   = restartedApp.GetTestClient();

        var response     = await restartedClient.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        var translations = (await response.Content.ReadFromJsonAsync<TranslationResponse[]>(JsonOpts))!;

        translations.Should().Contain(t => t.Key == "Persistent" && t.Culture == "en" && t.Value == "survived");
    }

    [Fact]
    public async Task UpdateTranslations_MultipleLanguages_AllPersisted()
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
    public async Task UpdateTranslations_Overwrite_LastValueWins()
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
        translations.Should().Contain(t => t.Key == "Hello" && t.Culture == "pl" && t.Value == "Cześć");
    }

    [Fact]
    public async Task UpdateTranslations_DeletedEntry_PersistedAcrossRestart()
    {
        await PostUpdate(translations: [("ToDelete", "en", "will be removed")]);
        await PostUpdate(keysToDelete: [("ToDelete", "en")]);

        await using var restartedApp = await BuildAppAsync();
        using var restartedClient   = restartedApp.GetTestClient();

        var response     = await restartedClient.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        var translations = (await response.Content.ReadFromJsonAsync<TranslationResponse[]>(JsonOpts))!;

        translations.Should().NotContain(t => t.Key == "ToDelete");
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

    private async Task<WebApplication> BuildAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl", "de"];
            x.Options.DefaultCulture    = "en";
            x.AddEditor();
            x.UsePostgreSQL(_fixture.ConnectionString);
        });

        var app = builder.Build();
        app.UseCultureWay("/translations");
        await app.InitializeCultureWayAsync();
        await app.StartAsync();
        return app;
    }

    private sealed record TranslationResponse(string Key, string Culture, string Value);
    private sealed record CultureResponse(string Code, bool IsDefault);
}
