# Kododo.CultureWay

Runtime localization editor for ASP.NET Core.

Persist and edit translated strings through a built-in web UI without restarting the application. Integrates with the standard `IStringLocalizer` / `IStringLocalizerFactory` pipeline.

## Quick start

```csharp
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources"); // only if you use .resx

builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["pl", "en", "de"];
    x.Options.DefaultCulture = "pl";
    x.AddEditor();                        // Kododo.CultureWay.UI
    x.UseResources<SharedResources>();    // optional: .resx as a read-only baseline
    x.UsePostgreSQL(connectionString);    // optional: Kododo.CultureWay.PostgreSQL
});

var app = builder.Build();

await app.InitializeCultureWayAsync();

app.UseRequestLocalization();
app.UseCultureWay("/translations").RequireAuthorization("Admin");
```

Without a persistence package, translations are kept in memory and lost on restart.

> **Security:** the editor is unauthenticated by default. Always protect it, e.g. with `RequireAuthorization`.

Full documentation: https://github.com/kododo-dev/CultureWay
