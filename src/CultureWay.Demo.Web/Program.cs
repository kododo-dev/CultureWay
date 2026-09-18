using Kododo.CultureWay;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Demo.Web;
using Kododo.CultureWay.PostgreSQL;
using Kododo.CultureWay.UI;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DemoDB");

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["pl", "en", "de"];
    x.Options.DefaultCulture = "en";
    x.AddEditor();
    // Surfaces Resources/DemoResources*.resx as read-only baseline translations — no custom
    // IReadOnlySource needed. Empty prefix keeps the existing "App.Title" style keys unprefixed.
    x.UseResources<DemoResources>(prefix: "");
    if (connectionString is not null)
        x.UsePostgreSQL(connectionString);
});

builder.Services.AddRequestLocalization(options =>
{
    options.SetDefaultCulture("en")
           .AddSupportedCultures("pl", "en", "de")
           .AddSupportedUICultures("pl", "en", "de");
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider { QueryStringKey = "culture" },
        new AcceptLanguageHeaderRequestCultureProvider(),
    ];
});

var app = builder.Build();

var pathBase = builder.Configuration["ASPNETCORE_PATHBASE"] ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

await app.InitializeCultureWayAsync();
await SeedIfEmptyAsync(app);

app.UseRequestLocalization();

app.MapGet("/", (IStringLocalizer<Program> localizer, HttpContext ctx, CultureWayOptions options) =>
{
    var culture = ctx.Features.Get<IRequestCultureFeature>();
    var currentCulture = culture?.RequestCulture.UICulture.Name ?? "en";
    var html = HomeView.Render(pathBase, localizer, currentCulture, options.SupportedCultures);
    return Results.Content(html, "text/html");
});

app.UseCultureWay("/translations");

app.Run();

static async Task SeedIfEmptyAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var store = scope.ServiceProvider.GetRequiredService<IStore>();
    var existing = await store.GetAllAsync();
    if (existing.Count > 0)
        return;

    await store.SetAsync(DemoTranslations.Seed());
    await app.InitializeCultureWayAsync();
}
