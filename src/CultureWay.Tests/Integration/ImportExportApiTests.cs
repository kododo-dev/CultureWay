using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

        Assert.Null(file.Error);
        Assert.Equal("translations-pl.json", file.FileName);
        Assert.Equal("application/json", file.ContentType);
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        Assert.Equal(new Dictionary<string, string>
        {
            ["Checkout.Pay"] = "Zapłać",
            ["Common.Save"] = "Zapisz",
        }, JsonSerializer.Deserialize<Dictionary<string, string>>(json));
        // Non-Latin text stays readable.
        Assert.Contains("Zapłać", json);
    }

    [Fact]
    public async Task ExportJson_RequiresExactlyOneLanguage()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "json", cultures = Array.Empty<string>() });

        Assert.Contains("exactly one language", file.Error);
    }

    [Fact]
    public async Task ExportXlsx_ReadsBackAsUnchanged()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "xlsx" });
        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport",
            Import(file.FileName, Convert.FromBase64String(file.ContentBase64)));

        Assert.Equal("translations.xlsx", file.FileName);
        Assert.Null(preview.Error);
        Assert.Empty(preview.Changes);
        Assert.Equal(Seed.Length, preview.Unchanged);
        Assert.Empty(preview.Problems);
    }

    [Fact]
    public async Task Export_FiltersByNamespaceAndByMissingLanguage()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations",
            new { format = "json", cultures = new[] { "en" }, @namespace = "Checkout", missingIn = "pl" });

        Assert.Equal("translations-en-Checkout-missing-pl.json", file.FileName);
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        Assert.Equal(new Dictionary<string, string>
        {
            ["Checkout.Pay.Tooltip"] = "You will be redirected",
        }, JsonSerializer.Deserialize<Dictionary<string, string>>(json));
    }

    [Fact]
    public async Task Export_EmptyNamespaceMeansKeysWithoutOne()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations",
            new { format = "json", cultures = new[] { "en" }, @namespace = "" });

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(file.ContentBase64));
        Assert.Equal("Title", Assert.Single(JsonSerializer.Deserialize<Dictionary<string, string>>(json)!).Key);
    }

    [Fact]
    public async Task Export_UnknownLanguage_IsAnError()
    {
        await using var app = await StartAsync();

        var file = await Call<ExportFileDto>(app.Client, "ExportTranslations", new { format = "xlsx", cultures = new[] { "fr" } });

        Assert.Contains("'fr'", file.Error);
        Assert.Empty(file.ContentBase64);
    }

    // ── Preview ────────────────────────────────────────────────────────

    [Fact]
    public async Task PreviewJson_ReportsAddedChangedAndUnchanged_WithoutWriting()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", Json("""
            { "Checkout.Pay": "Zapłać teraz", "Common.Save": "Zapisz", "Title": "Sklep" }
            """), culture: "pl"));

        Assert.Null(preview.Error);
        Assert.Equivalent(new[]
        {
            new ImportChangeDto("Checkout.Pay", "pl", "Zapłać", "Zapłać teraz"),
            new ImportChangeDto("Title", "pl", null, "Sklep"),
        }, preview.Changes, strict: true);
        Assert.Equal(1, preview.Unchanged);
        Assert.Equal(0, app.Store.SetCalls);
    }

    [Fact]
    public async Task PreviewJson_SkipsNestedAndNonStringValues()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("en.json", Json("""
            { "Checkout": { "Pay": "Pay" }, "Count": 3, "Empty": null, "Title": "Store" }
            """), culture: "en"));

        Assert.Equal("Title", Assert.Single(preview.Changes).Key);
        Assert.Equivalent(new[] { "Checkout", "Count", "Empty" }, preview.Problems.Select(p => p.Location), strict: true);
        Assert.Contains("Nested objects", preview.Problems.Single(p => p.Location == "Checkout").Reason);
    }

    [Fact]
    public async Task PreviewJson_AcceptsAByteOrderMark()
    {
        await using var app = await StartAsync();
        byte[] content = [0xEF, 0xBB, 0xBF, .. Json("""{ "Title": "Laden" }""")];

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("de.json", content, culture: "de"));

        Assert.Null(preview.Error);
        Assert.Single(preview.Changes);
    }

    [Theory]
    [InlineData("pl.json", null, "Choose the language")]
    [InlineData("pl.json", "fr", "not one of the editor's languages")]
    [InlineData("pl.csv", "pl", "Only .json and .xlsx")]
    public async Task Preview_RejectsMissingOrUnknownLanguageAndOtherFormats(string fileName, string? culture, string error)
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import(fileName, Json("{}"), culture));

        Assert.Contains(error, preview.Error);
    }

    [Fact]
    public async Task PreviewJson_InvalidJson_IsAnError()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", Json("{ nope"), culture: "pl"));

        Assert.StartsWith("The file is not valid JSON", preview.Error);
    }

    [Fact]
    public async Task PreviewXlsx_MatchesLanguageHeadersIgnoringCase_AndReportsUnknownOnes()
    {
        await using var app = await StartAsync();
        var content = Xlsx(["EN", "fr"],
            ("Title", new() { ["EN"] = "Store", ["fr"] = "Boutique" }));

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", content));

        Assert.Equal(new ImportChangeDto("Title", "en", "Shop", "Store"), Assert.Single(preview.Changes));
        Assert.Equal(new[] { "fr" }, preview.UnknownCultures);
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

        Assert.Equal("Store", Assert.Single(preview.Changes).NewValue);
        Assert.Equivalent(new[]
        {
            new ImportProblemDto("Row 3", "\"Title\" (en) appears more than once. The first one is used."),
            new ImportProblemDto("Row 4", "The key \" Padded\" starts or ends with a space."),
            new ImportProblemDto("Row 5", "The key is empty."),
        }, preview.Problems, strict: true);
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

        Assert.Contains("\"key\" column", preview.Error);
    }

    [Fact]
    public async Task Preview_NotAnExcelFile_IsAnError()
    {
        await using var app = await StartAsync();

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("t.xlsx", Json("not a workbook")));

        Assert.Contains("not a readable Excel workbook", preview.Error);
    }

    [Fact]
    public async Task Preview_LargerThanTheLimit_IsAnError()
    {
        await using var app = await StartAsync(o => o.MaxImportBytes = 100);

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport",
            Import("pl.json", Json($$"""{ "Title": "{{new string('a', 200)}}" }"""), culture: "pl"));

        Assert.Equal("The file is larger than 0.1 KB.", preview.Error);
    }

    // ── Apply ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_WritesEveryChangeInOneStoreCall()
    {
        await using var app = await StartAsync();

        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", Json("""
            { "Checkout.Pay": "Zapłać teraz", "Common.Save": "Zapisz", "Title": "Sklep" }
            """), culture: "pl"));

        Assert.Equal(new ImportResultDto(null, Added: 1, Updated: 1), result);
        Assert.Equal(1, app.Store.SetCalls);
        var all = await app.Store.GetAllAsync();
        Assert.Contains(new Translation("Checkout.Pay", "pl", "Zapłać teraz"), all);
        Assert.Contains(new Translation("Title", "pl", "Sklep"), all);
    }

    [Fact]
    public async Task Apply_OnlyMissing_KeepsExistingValues()
    {
        await using var app = await StartAsync();
        var content = Json("""{ "Checkout.Pay": "Zapłać teraz", "Title": "Sklep" }""");

        var preview = await Call<ImportPreviewDto>(app.Client, "PreviewImport", Import("pl.json", content, "pl", onlyMissing: true));
        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", content, "pl", onlyMissing: true));

        Assert.Equal(1, preview.Kept);
        Assert.Equal(new ImportResultDto(null, Added: 1, Updated: 0), result);
        Assert.Contains(new Translation("Checkout.Pay", "pl", "Zapłać"), await app.Store.GetAllAsync());
    }

    [Fact]
    public async Task Apply_NothingToChange_DoesNotWrite()
    {
        await using var app = await StartAsync();

        var result = await Call<ImportResultDto>(app.Client, "ApplyImport", Import("pl.json", Json("""{ "Common.Save": "Zapisz" }"""), "pl"));

        Assert.Equal(new ImportResultDto(null, 0, 0), result);
        Assert.Equal(0, app.Store.SetCalls);
    }

    [Fact]
    public async Task Apply_IsServedByTheEditorRightAway()
    {
        await using var app = await StartAsync();

        await Call<ImportResultDto>(app.Client, "ApplyImport", Import("de.json", Json("""{ "Title": "Laden" }"""), "de"));
        var translations = await Call<TranslationDto[]>(app.Client, "GetTranslations", new { });

        Assert.Contains(translations, t => t.Key == "Title" && t.Culture == "de" && t.Value == "Laden");
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

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, app.Store.SetCalls);
    }

    [Fact]
    public async Task Import_WithPermission_Works_AndExportNeedsNone()
    {
        await using var app = await StartAsync(o => o.CanImport = IsEditor);

        var import = await Post(app.Client, "ApplyImport", Import("pl.json", Json("""{ "Title": "Sklep" }"""), "pl"), role: "Editor");
        var export = await Post(app.Client, "ExportTranslations", new { format = "xlsx" }, role: "Viewer");

        Assert.Equal(HttpStatusCode.OK, import.StatusCode);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
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

        Assert.False(viewer.CanImport);
        Assert.Equal(1000, viewer.MaxImportBytes);
    }
}
