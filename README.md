# CultureWay

[![CI](https://github.com/kododo-dev/CultureWay/actions/workflows/ci.yml/badge.svg)](https://github.com/kododo-dev/CultureWay/actions/workflows/ci.yml)
[![Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://kododo.dev/cultureway/demo)

Runtime localization editor for ASP.NET Core. Translations are edited in a web UI built into your app and take effect immediately, without a rebuild or restart. Strings are resolved through the standard `IStringLocalizer`, so existing code keeps working.

A live demo is available at [kododo.dev/cultureway/demo](https://kododo.dev/cultureway/demo).

## UI

![Overview](docs/screenshots/01-overview.png)

![Manage languages](docs/screenshots/02-languages.png)

![Light theme with hidden languages](docs/screenshots/03-light-hidden-languages.png)

## Packages

| Package | NuGet | Description |
|---|---|---|
| `Kododo.CultureWay` | [![NuGet](https://img.shields.io/nuget/vpre/Kododo.CultureWay)](https://www.nuget.org/packages/Kododo.CultureWay) | Core DI registration, localizer, cache, in-memory store and `.resx` support |
| `Kododo.CultureWay.Core` | [![NuGet](https://img.shields.io/nuget/vpre/Kododo.CultureWay.Core)](https://www.nuget.org/packages/Kododo.CultureWay.Core) | Abstractions (`IStore`, `IReadOnlySource`, `Translation`) for custom backends |
| `Kododo.CultureWay.UI` | [![NuGet](https://img.shields.io/nuget/vpre/Kododo.CultureWay.UI)](https://www.nuget.org/packages/Kododo.CultureWay.UI) | Embedded web UI |
| `Kododo.CultureWay.PostgreSQL` | [![NuGet](https://img.shields.io/nuget/vpre/Kododo.CultureWay.PostgreSQL)](https://www.nuget.org/packages/Kododo.CultureWay.PostgreSQL) | PostgreSQL persistence store |

Targets `net8.0`, `net9.0` and `net10.0`.

## Quick start

```bash
dotnet add package Kododo.CultureWay.UI
dotnet add package Kododo.CultureWay.PostgreSQL  # optional, without it translations live in memory
```

```csharp
builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["en", "pl", "de"];
    x.Options.DefaultCulture = "en";
    x.AddEditor();
    x.UsePostgreSQL(connectionString);
});

var app = builder.Build();

await app.InitializeCultureWayAsync();

app.UseRequestLocalization();
app.UseCultureWay("/translations"); // mounts the UI at /translations
```

Then use localization as usual:

```csharp
app.MapGet("/", (IStringLocalizer<Program> l) => l["App.Title"].Value);
```

`InitializeCultureWayAsync` creates the database schema, loads the translations into the cache and restores languages added at runtime.

## Languages

Languages can be added, deleted and set as default from the UI, and the store keeps them across restarts. Each browser can also hide languages it doesn't need. That only affects which columns the editor shows and is remembered in the browser's local storage.

Cultures added at runtime are also registered in ASP.NET Core's `RequestLocalizationOptions`, so the request localization middleware picks them up without a restart.

## `.resx` files

Existing resource files can be shown in the editor as a read-only baseline:

```csharp
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");

builder.Services.AddCultureWay(x =>
{
    x.UseResources<SharedResources>();
});
```

`UseResources<T>()` uses the same naming convention as `IStringLocalizer<T>`. Keys are prefixed with the type name (`SharedResources.Title`). Pass `prefix: ""` to use the raw keys or any other string to choose your own.

A key that comes from `.resx` can't be deleted. You can override its value, and later reset it to the value from the resource file.

## Security

The editor is not protected by default. Anyone who can reach the route can change or delete translations, so restrict it before exposing the app:

```csharp
app.UseCultureWay("/translations").RequireAuthorization("Admin");
```

To let only some of the users who can open the editor add or delete languages, set `CanManageCultures` (see below). The others can still edit translations and hide languages, and the API answers their language changes with 403.

## Fitting the editor into your app

By default the editor is a standalone page. `AddEditor` takes options that make it part of your application:

```csharp
x.AddEditor(editor =>
{
    editor.Title    = "My app";       // top of the side menu and the browser tab
    editor.HomeUrl  = "/";            // the title links here
    editor.Language = "pl";           // editor's own interface: en or pl; empty follows the browser
    editor.Links    = ctx => ctx.User.IsInRole("Admin")
        ? [new EditorLink("Users", "/admin/users") { Icon = EditorLinkIcons.Users }]
        : [];
    editor.User     = ctx => ctx.User.Identity?.IsAuthenticated == true
        ? new EditorUser(ctx.User.Identity.Name!) { AccountUrl = "/account", SignOutUrl = "/logout" }
        : null;
    editor.CanManageCultures = ctx => ctx.User.IsInRole("Admin");
});
```

The settings that take an `HttpContext` run on every request, so they can depend on who is signed in. Links and the user appear at the bottom of the side menu. A link can show one of the icons in `EditorLinkIcons` (home, translations, users, key); without one it shows an arrow. The link to the page the editor is on is highlighted, so a host can list the editor itself among its own pages. The sign-out button submits a plain form POST to `SignOutUrl` without an antiforgery token, so that endpoint has to accept one.

The editor also remembers the light or dark theme in `localStorage` under `cultureway.theme` (`light` or `dark`). Your own pages on the same origin can read that key to match it. When a request fails because the session expired (a 401, or a redirect to a sign-in page), the editor asks the user to sign in again in a new tab, so edits that were not saved stay on the page.

## Custom store

Implement `IStore` from `Kododo.CultureWay.Core` and register it before `AddCultureWay`. A minimal store needs `InitializeAsync`, `GetAllAsync`, `SetAsync` and `DeleteAsync`.

The members that deal with languages (`GetSupportedCulturesAsync`, `AddSupportedCultureAsync`, `RemoveSupportedCultureAsync`, `GetDefaultCultureAsync`, `SetDefaultCultureAsync`) have default implementations that do nothing. Override them if you want languages changed in the UI to survive a restart.

## Running the demo

```bash
cd src/CultureWay.UI/SPA && npm ci && npm run build
cd ../../..
dotnet run --project src/CultureWay.Demo.Web --framework net10.0
```

Without a `DemoDB` connection string the demo uses the in-memory store.

## Building from source

```bash
cd src/CultureWay.UI/SPA && npm ci && npm run build
cd ../../..
dotnet test src/CultureWay.Tests
dotnet test src/CultureWay.PostgreSQL.Tests  # needs Docker (Testcontainers)
```

## License

[MIT](LICENSE)
