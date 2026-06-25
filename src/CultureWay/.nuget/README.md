# Kododo.CultureWay

Runtime localization editor for ASP.NET Core.

Persist and edit translated strings through a built-in web UI without restarting the application. Integrates with the standard `IStringLocalizer` / `IStringLocalizerFactory` pipeline.

## Quick start

```csharp
builder.Services.AddCultureWay(x =>
{
    x.Options.SupportedCultures = ["pl", "en", "de"];
    x.Options.DefaultCulture = "pl";
    x.AddEditor();
    x.UsePostgreSQL(connectionString);
});

// ...

app.UseCultureWay("/translations");
await app.InitializeCultureWayDatabaseAsync();
```
