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

The editor lets you edit translations, add/delete languages, choose the default language, and hide languages per browser. It is not protected by default, so restrict access before exposing the app.

Full documentation: https://github.com/kododo-dev/CultureWay
