using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kododo.CultureWay.UI;
using Kododo.CultureWay.UI.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Kododo.CultureWay.Tests.Integration;

public class EditorSettingsApiTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static async Task<WebApplication> StartAsync(Action<EditorOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl"];
            x.Options.DefaultCulture    = "en";
            if (configure is null)
                x.AddEditor();
            else
                x.AddEditor(configure);
        });

        var app = builder.Build();
        app.UseCultureWay("/translations");
        await app.InitializeCultureWayAsync();
        await app.StartAsync();
        return app;
    }

    private static async Task<EditorSettingsDto> FetchSettings(HttpClient client, string? role = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/translations/api/GetEditorSettings")
        {
            Content = JsonContent.Create(new { }),
        };
        if (role is not null)
            request.Headers.Add("X-Role", role);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EditorSettingsDto>(JsonOpts))!;
    }

    private static bool IsAdmin(HttpContext ctx) => ctx.Request.Headers["X-Role"] == "Admin";

    [Fact]
    public async Task WithoutOptions_ReturnsDefaults()
    {
        await using var app = await StartAsync();
        var settings = await FetchSettings(app.GetTestClient());

        Assert.Null(settings.Title);
        Assert.Null(settings.HomeUrl);
        Assert.Null(settings.Language);
        Assert.Empty(settings.Links);
        Assert.Null(settings.User);
        Assert.True(settings.CanManageCultures);
    }

    [Fact]
    public async Task ReturnsConfiguredValues()
    {
        await using var app = await StartAsync(o =>
        {
            o.Title    = "Polyglot";
            o.HomeUrl  = "/";
            o.Language = "pl";
            o.Links    = _ => [new EditorLink("Users", "/admin/users") { Icon = EditorLinkIcons.Users }];
            o.User     = _ => new EditorUser("Ada") { AccountUrl = "/account", SignOutUrl = "/logout" };
        });
        var settings = await FetchSettings(app.GetTestClient());

        Assert.Equal("Polyglot", settings.Title);
        Assert.Equal("/", settings.HomeUrl);
        Assert.Equal("pl", settings.Language);
        Assert.Equal(new EditorLinkDto("Users", "/admin/users", "users"), Assert.Single(settings.Links));
        Assert.Equal(new EditorUserDto("Ada", "/account", "/logout"), settings.User);
    }

    [Fact]
    public async Task PerRequestSettings_DependOnTheRequest()
    {
        await using var app = await StartAsync(o =>
        {
            o.Links             = ctx => IsAdmin(ctx) ? [new EditorLink("Users", "/admin/users")] : [];
            o.CanManageCultures = IsAdmin;
        });
        var client = app.GetTestClient();

        var admin  = await FetchSettings(client, "Admin");
        var editor = await FetchSettings(client, "Editor");

        Assert.Single(admin.Links);
        Assert.True(admin.CanManageCultures);
        Assert.Empty(editor.Links);
        Assert.False(editor.CanManageCultures);
    }

    [Theory]
    [InlineData("AddCulture", "{\"culture\":\"fr\"}")]
    [InlineData("DeleteCulture", "{\"culture\":\"pl\"}")]
    [InlineData("SetDefaultCulture", "{\"culture\":\"pl\"}")]
    public async Task CultureManagement_WhenNotAllowed_Returns403AndChangesNothing(string request, string body)
    {
        await using var app = await StartAsync(o => o.CanManageCultures = IsAdmin);
        var client = app.GetTestClient();

        var response = await client.PostAsync($"/translations/api/{request}",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var cultures = await (await client.PostAsJsonAsync("/translations/api/GetCultures", new { }))
            .Content.ReadFromJsonAsync<CultureDto[]>(JsonOpts);
        Assert.Equivalent(new[] { "en", "pl" }, cultures!.Select(c => c.Code), strict: true);
        Assert.Equal("en", cultures!.Single(c => c.IsDefault).Code);
    }

    [Fact]
    public async Task CultureManagement_WhenAllowed_Works()
    {
        await using var app = await StartAsync(o => o.CanManageCultures = IsAdmin);
        var request = new HttpRequestMessage(HttpMethod.Post, "/translations/api/AddCulture")
        {
            Content = new StringContent("{\"culture\":\"fr\"}", System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Role", "Admin");

        var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<string[]>(JsonOpts))!);
    }

    [Fact]
    public async Task OtherRequests_AreNotAffectedByCultureManagement()
    {
        await using var app = await StartAsync(o => o.CanManageCultures = _ => false);

        var response = await app.GetTestClient().PostAsJsonAsync("/translations/api/GetTranslations", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
