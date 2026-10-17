# Kododo.CultureWay.UI

Embedded web UI for CultureWay.

Adds a React-based localization editor served directly from the host application, so no separate deployment is needed.

## Usage

```csharp
builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["en", "pl"];
    x.AddEditor();
});

var app = builder.Build();
await app.InitializeCultureWayAsync();

app.UseCultureWay("/translations").RequireAuthorization("Admin");
```

The editor lets you edit translations, add/delete languages, choose the default language, and hide languages per browser. Outside the Development environment it answers `403` until you decide who may use it (`RequireAuthorization(...)`, or `AllowAnonymous()` for a public demo).

The editor runs in [Kododo.Way.Shell](https://www.nuget.org/packages/Kododo.Way.Shell): the title, home link, links, signed-in user and interface language are set with `services.AddWayShell(o => ...)`, shared with the other Ways in the app. `AddEditor(editor => ...)` keeps what only the editor needs: per-request links, `CanManageCultures`, `CanImport` and `MaxImportBytes`.

Full documentation: https://github.com/kododo-dev/CultureWay
