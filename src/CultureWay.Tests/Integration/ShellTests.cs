using System.Net;
using Kododo.CultureWay.UI;
using Kododo.Way.Shell;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kododo.CultureWay.Tests.Integration;

/// <summary>The editor runs in Way.Shell: the shell's page, the editor's module, the shell's protection.</summary>
public class ShellTests
{
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(
        Action<RouteGroupBuilder>? decide, Action<EditorOptions>? editor = null, Action<ShellOptions>? shell = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl"];
            x.Options.DefaultCulture = "en";
            x.AddEditor(editor ?? (_ => { }));
        });
        if (shell is not null) builder.Services.AddWayShell(shell);

        var app = builder.Build();
        var group = app.UseCultureWay("/i18n");
        decide?.Invoke(group);
        await app.InitializeCultureWayAsync();
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    [Fact]
    public async Task Page_IsTheShellWithTheEditorsModule()
    {
        var (app, client) = await StartAsync(g => g.AllowAnonymous());
        await using var _ = app;

        var html = await client.GetStringAsync("/i18n/");

        Assert.Contains("""<base href="/i18n/">""", html);
        Assert.Contains("""<script type="importmap">""", html);
        Assert.Contains("\"moduleUrl\":\"/i18n/_way/way.js?v=", html);
        Assert.Contains("\"apiBase\":\"/i18n/api\"", html);
        Assert.Contains("<title>Translations</title>", html);
    }

    [Fact]
    public async Task Module_IsServedFromTheEditorsAssembly()
    {
        var (app, client) = await StartAsync(g => g.AllowAnonymous());
        await using var _ = app;

        var module = await client.GetAsync("/i18n/_way/way.js");

        Assert.Equal(HttpStatusCode.OK, module.StatusCode);
        Assert.Contains("contract:2", (await module.Content.ReadAsStringAsync()).Replace(" ", ""));
    }

    [Theory]
    [InlineData("/i18n/")]
    [InlineData("/i18n/_way/way.js")]
    public async Task WithoutADecisionOnAuthorization_ItIsForbidden_OutsideDevelopment(string url)
    {
        var (app, client) = await StartAsync(decide: null);
        await using var _ = app;

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/i18n/api/GetTranslations", Json())).StatusCode);
    }

    [Fact]
    public async Task ObsoleteEditorOptions_AreShownByTheShell()
    {
#pragma warning disable CS0618 // passed on to ShellOptions for hosts that still set them
        var (app, client) = await StartAsync(g => g.AllowAnonymous(), editor: o =>
        {
            o.Title = "Polyglot";
            o.HomeUrl = "/";
            o.Language = "pl";
            o.User = _ => new EditorUser("Ada") { AccountUrl = "/account", SignOutUrl = "/logout" };
            o.Links = ctx => ctx.Request.Query.ContainsKey("admin") ? [new EditorLink("Users", "/admin/users") { Icon = EditorLinkIcons.Users }] : [];
        });
#pragma warning restore CS0618
        await using var _ = app;

        var html = await client.GetStringAsync("/i18n/");
        var admin = await client.GetStringAsync("/i18n/?admin");

        Assert.Contains("<title>Polyglot</title>", html);
        Assert.Contains("""<html lang="pl">""", html);
        Assert.Contains("\"homeUrl\":\"/\"", html);
        Assert.Contains("\"user\":{\"name\":\"Ada\",\"accountUrl\":\"/account\",\"signOutUrl\":\"/logout\"}", html);
        // Per-request links go to the shell's menu.
        Assert.Contains("\"links\":[]", html);
        Assert.Contains("\"links\":[{\"title\":\"Users\",\"url\":\"/admin/users\",\"icon\":\"users\"}]", admin);
    }

    [Fact]
    public async Task ShellOptions_SetByTheHost_WinOverUnsetEditorOptions()
    {
        var (app, client) = await StartAsync(g => g.AllowAnonymous(), shell: o => o.Title = "Acme");
        await using var _ = app;

        Assert.Contains("<title>Acme</title>", await client.GetStringAsync("/i18n/"));
    }

    private static StringContent Json() => new("{}", System.Text.Encoding.UTF8, "application/json");
}
