using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Kododo.CultureWay.Core.Model;
using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Kododo.CultureWay.Tests.Integration;

public class ReadOnlySourceApiTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        var source = Substitute.For<IReadOnlySource>();
        source.GetDefaultsAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<IReadOnlyList<Translation>>(
            [
                new Translation("App.Title", "en", "Title"),
                new Translation("App.Title", "pl", "Tytul"),
            ]));

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCultureWay(x =>
        {
            x.Options.SupportedCultures = ["en", "pl"];
            x.Options.DefaultCulture = "en";
            x.AddEditor();
            x.Services.AddSingleton(source);
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

    [Fact]
    public async Task GetTranslations_IncludesSourceKeysWithExternalDefault()
    {
        var result = await FetchTranslations();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.HasExternalDefault.Should().BeTrue());
        result.Single(t => t.Culture == "pl").Value.Should().Be("Tytul");
    }

    [Fact]
    public async Task UpdateTranslations_OverrideSourceValue_ReturnsOverrideAndKeepsDefault()
    {
        await PostUpdate(translations: [("App.Title", "en", "Custom")]);

        var en = (await FetchTranslations()).Single(t => t.Culture == "en");

        en.Value.Should().Be("Custom");
        en.HasExternalDefault.Should().BeTrue();
        en.ExternalDefaultValue.Should().Be("Title");
    }

    [Fact]
    public async Task UpdateTranslations_DeleteOverride_RestoresSourceValue()
    {
        await PostUpdate(translations: [("App.Title", "en", "Custom")]);

        await PostUpdate(keysToDelete: [("App.Title", "en")]);

        var en = (await FetchTranslations()).Single(t => t.Culture == "en");
        en.Value.Should().Be("Title");
        en.HasExternalDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTranslations_DeleteKeyWithoutOverride_SourceKeyStillReturned()
    {
        await PostUpdate(keysToDelete: [("App.Title", "en"), ("App.Title", "pl")]);

        var result = await FetchTranslations();

        result.Should().HaveCount(2);
    }

    private async Task<TranslationResponse[]> FetchTranslations()
    {
        var response = await _client.PostAsJsonAsync("/translations/api/GetTranslations", new { });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TranslationResponse[]>(JsonOpts))!;
    }

    private Task<HttpResponseMessage> PostUpdate(
        (string Key, string Culture, string Value)[]? translations = null,
        (string Key, string Culture)[]? keysToDelete = null)
    {
        var payload = new
        {
            Translations = (translations ?? []).Select(t => new { t.Key, t.Culture, t.Value }).ToArray(),
            KeysToDelete = (keysToDelete ?? []).Select(k => new { k.Key, k.Culture }).ToArray(),
        };
        return _client.PostAsync(
            "/translations/api/UpdateTranslations",
            new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json"));
    }

    private sealed record TranslationResponse(
        string Key, string Culture, string Value, bool HasExternalDefault, string? ExternalDefaultValue);
}
