using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<EditorSettingsDto>(JsonOpts))!;
    }

    private static bool IsAdmin(HttpContext ctx) => ctx.Request.Headers["X-Role"] == "Admin";

    [Fact]
    public async Task WithoutOptions_ReturnsDefaults()
    {
        await using var app = await StartAsync();
        var settings = await FetchSettings(app.GetTestClient());

        settings.Title.Should().BeNull();
        settings.HomeUrl.Should().BeNull();
        settings.Language.Should().BeNull();
        settings.Links.Should().BeEmpty();
        settings.User.Should().BeNull();
        settings.CanManageCultures.Should().BeTrue();
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

        settings.Title.Should().Be("Polyglot");
        settings.HomeUrl.Should().Be("/");
        settings.Language.Should().Be("pl");
        settings.Links.Should().ContainSingle().Which.Should().Be(new EditorLinkDto("Users", "/admin/users", "users"));
        settings.User.Should().Be(new EditorUserDto("Ada", "/account", "/logout"));
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

        admin.Links.Should().HaveCount(1);
        admin.CanManageCultures.Should().BeTrue();
        editor.Links.Should().BeEmpty();
        editor.CanManageCultures.Should().BeFalse();
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

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var cultures = await (await client.PostAsJsonAsync("/translations/api/GetCultures", new { }))
            .Content.ReadFromJsonAsync<CultureDto[]>(JsonOpts);
        cultures!.Select(c => c.Code).Should().BeEquivalentTo("en", "pl");
        cultures!.Single(c => c.IsDefault).Code.Should().Be("en");
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

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<string[]>(JsonOpts)).Should().BeEmpty();
    }

    [Fact]
    public async Task OtherRequests_AreNotAffectedByCultureManagement()
    {
        await using var app = await StartAsync(o => o.CanManageCultures = _ => false);

        var response = await app.GetTestClient().PostAsJsonAsync("/translations/api/GetTranslations", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
