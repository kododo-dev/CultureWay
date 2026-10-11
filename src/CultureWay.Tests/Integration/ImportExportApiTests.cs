using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI;
using Kododo.CultureWay.UI.DTO;
using Kododo.CultureWay.UI.ImportExport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kododo.CultureWay.Tests.Integration;

public class ImportExportApiTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static readonly Translation[] Seed =
    [
        new("Checkout.Pay", "en", "Pay now"),
        new("Checkout.Pay", "pl", "Zapłać"),
        new("Checkout.Pay.Tooltip", "en", "You will be redirected"),
        new("Common.Save", "en", "Save"),
        new("Common.Save", "pl", "Zapisz"),
        new("Common.Save", "de", "Speichern"),
        new("Title", "en", "Shop"),
    ];

    /// <summary>Counts writes, so a test can tell an import is one store call.</summary>
    private sealed class CountingStore : IStore
    {
        private readonly Dictionary<(string, string), string> _values = new();
        public int SetCalls { get; set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Translation>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Translation>>(
                _values.Select(v => new Translation(v.Key.Item1, v.Key.Item2, v.Value)).ToList());

        public Task SetAsync(IReadOnlyCollection<Translation> translations, CancellationToken cancellationToken = default)
        {
            SetCalls++;
            foreach (var t in translations)
                _values[(t.Key, t.Culture)] = t.Value;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(IReadOnlyCollection<(string Key, string Culture)> keys, CancellationToken cancellationToken = default)
        {
            foreach (var k in keys)
                _values.Remove(k);
            return Task.CompletedTask;
        }
    }

    private sealed record App(WebApplication Web, HttpClient Client, CountingStore Store) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Web.StopAsync();
            await Web.DisposeAsync();
        }
    }

    private static async Task<App> StartAsync(Action<EditorOptions>? configure = null)
    {
        var store = new CountingStore();
        await store.SetAsync(Seed);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IStore>(store);
        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl", "de"];
            x.Options.DefaultCulture    = "en";
            x.AddEditor(configure ?? (_ => { }));
        });

        var app = builder.Build();
        app.UseCultureWay("/translations");
        await app.InitializeCultureWayAsync();
        await app.StartAsync();

        // Leaves only the writes the test makes.
        store.SetCalls = 0;
        return new App(app, app.GetTestClient(), store);
    }

    // StringContent rather than PostAsJsonAsync: Reiho's binder needs a known Content-Length.
    private static async Task<HttpResponseMessage> Post(HttpClient client, string request, object body, string? role = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, $"/translations/api/{request}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json"),
        };
        if (role is not null)
            message.Headers.Add("X-Role", role);
        return await client.SendAsync(message);
    }

    private static async Task<T> Call<T>(HttpClient client, string request, object body)
    {
        var response = await Post(client, request, body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(JsonOpts))!;
    }

    private static object Import(string fileName, byte[] content, string? culture = null, bool onlyMissing = false) =>
        new { fileName, contentBase64 = Convert.ToBase64String(content), culture, onlyMissing };

    private static byte[] Json(string json) => Encoding.UTF8.GetBytes(json);

    private static byte[] Xlsx(string[] cultures, params (string Key, Dictionary<string, string> Values)[] rows) =>
        TranslationFiles.WriteXlsx(cultures, rows.Select(r => (r.Key, (IReadOnlyDictionary<string, string>)r.Values)));

    // ── Export ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportJson_IsFlatSortedAndLeavesOutKeysWithoutAValue()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "json", cultures = new[] { "pl" } });

        file.Error.Should().BeNull();
        file.FileName.Should().Be("translations-pl.json");
        file.ContentType.Should().Be("application/json");
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        JsonSerializer.Deserialize<Dictionary<string, string>>(json).Should().Equal(new Dictionary<string, string>
        {
            ["Checkout.Pay"] = "Zapłać",
            ["Common.Save"] = "Zapisz",
        });
        json.Should().Contain("Zapłać", "non-Latin text stays readable");
    }

    [Fact]
    public async Task ExportJson_RequiresExactlyOneLanguage()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "json", cultures = Array.Empty<string>() });

        file.Error.Should().Contain("exactly one language");
    }

    [Fact]
    public async Task ExportXlsx_ReadsBackAsUnchanged()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "xlsx" });
        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport",
            Import(file.FileName, Convert.FromBase64String(file.ContentBase64)));

        file.FileName.Should().Be("translations.xlsx");
        preview.Error.Should().BeNull();
        preview.Changes.Should().BeEmpty();
        preview.Unchanged.Should().Be(Seed.Length);
        preview.Problems.Should().BeEmpty();
    }

    [Fact]
    public async Task Export_FiltersByNamespaceAndByMissingLanguage()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations",
            new { format = "json", cultures = new[] { "en" }, @namespace = "Checkout", missingIn = "pl" });

        file.FileName.Should().Be("translations-en-Checkout-missing-pl.json");
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        JsonSerializer.Deserialize<Dictionary<string, string>>(json).Should().Equal(new Dictionary<string, string>
        {
            ["Checkout.Pay.Tooltip"] = "You will be redirected",
        });
    }

    [Fact]
    public async Task Export_EmptyNamespaceMeansKeysWithoutOne()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations",
            new { format = "json", cultures = new[] { "en" }, @namespace = "" });

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        JsonSerializer.Deserialize<Dictionary<string, string>>(json).Should().ContainSingle().Which.Key.Should().Be("Title");
    }

    [Fact]
    public async Task Export_UnknownLanguage_IsAnError()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "xlsx", cultures = new[] { "fr" } });

        file.Error.Should().Contain("'fr'");
        file.ContentBase64.Should().BeEmpty();
    }

    // ── Preview ────────────────────────────────────────────────────────

    [Fact]
    public async Task PreviewJson_ReportsAddedChangedAndUnchanged_WithoutWriting()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", Json("""
            { "Checkout.Pay": "Zapłać teraz", "Common.Save": "Zapisz", "Title": "Sklep" }
            """), culture: "pl"));

        preview.Error.Should().BeNull();
        preview.Changes.Should().BeEquivalentTo(new[]
        {
            new ImportChangeDto("Checkout.Pay", "pl", "Zapłać", "Zapłać teraz"),
            new ImportChangeDto("Title", "pl", null, "Sklep"),
        });
        preview.Unchanged.Should().Be(1);
        app.Store.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task PreviewJson_SkipsNestedAndNonStringValues()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("en.json", Json("""
            { "Checkout": { "Pay": "Pay" }, "Count": 3, "Empty": null, "Title": "Store" }
            """), culture: "en"));

        preview.Changes.Should().ContainSingle().Which.Key.Should().Be("Title");
        preview.Problems.Select(p => p.Location).Should().BeEquivalentTo("Checkout", "Count", "Empty");
        preview.Problems.Single(p => p.Location == "Checkout").Reason.Should().Contain("Nested objects");
    }

    [Fact]
    public async Task PreviewJson_AcceptsAByteOrderMark()
    {
        await using var app = await StartAsync();
        byte[] content = [0xEF, 0xBB, 0xBF, .. Json("""{ "Title": "Laden" }""")];

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("de.json", content, culture: "de"));

        preview.Error.Should().BeNull();
        preview.Changes.Should().ContainSingle();
    }

    [Theory]
    [InlineData("pl.json", null, "Choose the language")]
    [InlineData("pl.json", "fr", "not one of the editor's languages")]
    [InlineData("pl.csv", "pl", "Only .json and .xlsx")]
    public async Task Preview_RejectsMissingOrUnknownLanguageAndOtherFormats(string fileName, string? culture, string error)
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import(fileName, Json("{}"), culture));

        preview.Error.Should().Contain(error);
    }

    [Fact]
    public async Task PreviewJson_InvalidJson_IsAnError()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", Json("{ nope"), culture: "pl"));

        preview.Error.Should().StartWith("The file is not valid JSON");
    }

    [Fact]
    public async Task PreviewXlsx_MatchesLanguageHeadersIgnoringCase_AndReportsUnknownOnes()
    {
        await using var app = await StartAsync();
        var content = Xlsx(["EN", "fr"],
            ("Title", new() { ["EN"] = "Store", ["fr"] = "Boutique" }));

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", content));

        preview.Changes.Should().ContainSingle().Which.Should().Be(new ImportChangeDto("Title", "en", "Shop", "Store"));
        preview.UnknownCultures.Should().Equal("fr");
    }

    [Fact]
    public async Task PreviewXlsx_SkipsDuplicatesKeysWithSpacesAndRowsWithoutAKey()
    {
        await using var app = await StartAsync();
        var content = Xlsx(["en"],
            ("Title", new() { ["en"] = "Store" }),
            ("Title", new() { ["en"] = "Second" }),
            (" Padded", new() { ["en"] = "x" }),
            ("", new() { ["en"] = "orphan" }));

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", content));

        preview.Changes.Should().ContainSingle().Which.NewValue.Should().Be("Store");
        preview.Problems.Should().BeEquivalentTo(new[]
        {
            new ImportProblemDto("Row 3", "\"Title\" (en) appears more than once. The first one is used."),
            new ImportProblemDto("Row 4", "The key \" Padded\" starts or ends with a space."),
            new ImportProblemDto("Row 5", "The key is empty."),
        });
    }

    [Fact]
    public async Task PreviewXlsx_WithoutAKeyColumn_IsAnError()
    {
        await using var app = await StartAsync();
        // A sheet with language columns only.
        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, new[] { new Dictionary<string, object> { ["en"] = "Store" } },
            excelType: MiniExcelLibs.ExcelType.XLSX);

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", stream.ToArray()));

        preview.Error.Should().Contain("\"key\" column");
    }

    [Fact]
    public async Task Preview_NotAnExcelFile_IsAnError()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", Json("not a workbook")));

        preview.Error.Should().Contain("not a readable Excel workbook");
    }

    [Fact]
    public async Task Preview_LargerThanTheLimit_IsAnError()
    {
        await using var app = await StartAsync(o => o.MaxImportBytes = 100);

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport",
            Import("pl.json", Json($$"""{ "Title": "{{new string('a', 200)}}" }"""), culture: "pl"));

        preview.Error.Should().Be("The file is larger than 0.1 KB.");
    }

    // ── Apply ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_WritesEveryChangeInOneStoreCall()
    {
        await using var app = await StartAsync();

        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", Json("""
            { "Checkout.Pay": "Zapłać teraz", "Common.Save": "Zapisz", "Title": "Sklep" }
            """), culture: "pl"));

        result.Should().Be(new ImportResultDto(null, Added: 1, Updated: 1));
        app.Store.SetCalls.Should().Be(1);
        var all = await app.Store.GetAllAsync();
        all.Should().Contain(new Translation("Checkout.Pay", "pl", "Zapłać teraz"));
        all.Should().Contain(new Translation("Title", "pl", "Sklep"));
    }

    [Fact]
    public async Task Apply_OnlyMissing_KeepsExistingValues()
    {
        await using var app = await StartAsync();
        var content = Json("""{ "Checkout.Pay": "Zapłać teraz", "Title": "Sklep" }""");

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", content, "pl", onlyMissing: true));
        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", content, "pl", onlyMissing: true));

        preview.Kept.Should().Be(1);
        result.Should().Be(new ImportResultDto(null, Added: 1, Updated: 0));
        (await app.Store.GetAllAsync()).Should().Contain(new Translation("Checkout.Pay", "pl", "Zapłać"));
    }

    [Fact]
    public async Task Apply_NothingToChange_DoesNotWrite()
    {
        await using var app = await StartAsync();

        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", Json("""{ "Common.Save": "Zapisz" }"""), "pl"));

        result.Should().Be(new ImportResultDto(null, 0, 0));
        app.Store.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task Apply_IsServedByTheEditorRightAway()
    {
        await using var app = await StartAsync();

        await Call<ImportResultDto>(app.Client, "ApplyImport", Import("de.json", Json("""{ "Title": "Laden" }"""), "de"));
        var translations = await Call<TranslationDto[]>(app.Client, "GetTranslations", new { });

        translations.Should().Contain(t => t.Key == "Title" && t.Culture == "de" && t.Value == "Laden");
    }

    // ── Permission ─────────────────────────────────────────────────────

    private static bool IsEditor(HttpContext ctx) => ctx.Request.Headers["X-Role"] == "Editor";

    [Theory]
    [InlineData("PreviewImport")]
    [InlineData("ApplyImport")]
    public async Task Import_WithoutPermission_Returns403(string request)
    {
        await using var app = await StartAsync(o => o.CanImport = IsEditor);

        var response = await Post(app.Client, request, Import("pl.json", Json("""{ "Title": "Sklep" }"""), "pl"), role: "Viewer");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        app.Store.SetCalls.Should().Be(0);
    }

    [Fact]
    public async Task Import_WithPermission_Works_AndExportNeedsNone()
    {
        await using var app = await StartAsync(o => o.CanImport = IsEditor);

        var import = await Post(app.Client, "ApplyImport", Import("pl.json", Json("""{ "Title": "Sklep" }"""), "pl"), role: "Editor");
        var export = await Post(app.Client, "ExportTranslations", new { format = "xlsx" }, role: "Viewer");

        import.StatusCode.Should().Be(HttpStatusCode.OK);
        export.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_TellTheEditorWhetherItMayImportAndHowLarge()
    {
        await using var app = await StartAsync(o =>
        {
            o.CanImport = IsEditor;
            o.MaxImportBytes = 1000;
        });

        var viewer = await Call<EditorSettingsDto>(app.Client, "GetEditorSettings", new { });

        viewer.CanImport.Should().BeFalse();
        viewer.MaxImportBytes.Should().Be(1000);
    }
}
