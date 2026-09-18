# CultureWay

[![CI](https://github.com/kododo-dev/CultureWay/actions/workflows/ci.yml/badge.svg)](https://github.com/kododo-dev/CultureWay/actions/workflows/ci.yml)
[![Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://kododo.dev/cultureway/demo)

Runtime localization editor for ASP.NET Core. Edit translated strings in a built-in web UI and see the changes immediately — no rebuild, no restart. CultureWay plugs into the standard `IStringLocalizer` pipeline, so existing code keeps working.

A live demo is available at [kododo.dev/cultureway/demo](https://kododo.dev/cultureway/demo).

## Features

- **Web editor** embedded in your app (React SPA served from the host, no separate deployment).
- **Standard API** — resolves through `IStringLocalizer` / `IStringLocalizerFactory` / `IStringLocalizer<T>`.
- **Manage languages at runtime** — add, delete and change the default language from the UI; the choice is persisted by the store.
- **Per-browser language visibility** — hide languages you are not working on; only the editor columns are affected and the choice is remembered in the browser.
- **`.resx` as a read-only baseline** — surface existing resource files in the editor, override individual strings and reset them back to the `.resx` value.
- **Pluggable persistence** — in-memory by default, PostgreSQL out of the box, or implement `IStore` yourself.
- Targets `net8.0`, `net9.0` and `net10.0`.

## Packages

| Package | Purpose |
|---|---|
| `Kododo.CultureWay` | Core runtime: `AddCultureWay`, localizer, cache, in-memory store, `.resx` source |
| `Kododo.CultureWay.UI` | Embedded web editor (`AddEditor`, `UseCultureWay`) |
| `Kododo.CultureWay.PostgreSQL` | PostgreSQL persistence (`UsePostgreSQL`) |
| `Kododo.CultureWay.Core` | Abstractions only (`IStore`, `IReadOnlySource`, `Translation`) for custom backends |

## Quick start

```bash
dotnet add package Kododo.CultureWay.UI
dotnet add package Kododo.CultureWay.PostgreSQL   # optional, otherwise data lives in memory
```

```csharp
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources"); // only if you use .resx

builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["en", "pl", "de"];
    x.Options.DefaultCulture = "en";
    x.AddEditor();
    x.UseResources<SharedResources>();       // optional: .resx as read-only baseline
    x.UsePostgreSQL(connectionString);       // optional: persist edits
});

var app = builder.Build();

await app.InitializeCultureWayAsync();      // creates schema, loads cache, restores runtime languages

app.UseRequestLocalization();
app.UseCultureWay("/translations");          // editor at /translations
```

Then use localization as usual:

```csharp
app.MapGet("/", (IStringLocalizer<Program> l) => l["App.Title"].Value);
```

## Securing the editor

`UseCultureWay` returns a `RouteGroupBuilder`. **The editor is unauthenticated by default** and can change or delete your translations, so protect it before exposing the app:

```csharp
app.UseCultureWay("/translations").RequireAuthorization("Admin");
```

## `.resx` files as a baseline

`UseResources<T>()` surfaces the `.resx` files of `T` (same naming convention as `IStringLocalizer<T>`) in the editor.
Keys coming from `.resx` cannot be deleted, only overridden or reset to their original value. Keys are prefixed with the type name (`{TypeName}.{key}`); pass `prefix: ""` to use the raw keys, or any string of your own.

## Custom store

Implement `IStore` (from `Kododo.CultureWay.Core`) and register it before `AddCultureWay`. The culture-related members have default implementations, so a minimal store only needs `InitializeAsync`, `GetAllAsync`, `SetAsync` and `DeleteAsync`; override `GetSupportedCulturesAsync` / `AddSupportedCultureAsync` / `RemoveSupportedCultureAsync` / `GetDefaultCultureAsync` / `SetDefaultCultureAsync` to keep runtime language changes across restarts.

## Demo

A runnable sample lives in `src/CultureWay.Demo.Web`:

```bash
dotnet run --project src/CultureWay.Demo.Web --framework net10.0
```

The SPA must be built first (`cd src/CultureWay.UI/SPA && npm ci && npm run build`).

## Building from source

```bash
cd src/CultureWay.UI/SPA && npm ci && npm run build
cd ../../.. && dotnet test src/CultureWay.Tests
dotnet test src/CultureWay.PostgreSQL.Tests   # requires Docker (Testcontainers)
```

## License

[MIT](LICENSE)
