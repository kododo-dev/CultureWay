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

`AddEditor(editor => ...)` fits the editor into your application: a title and home link, links to your other pages, the signed-in user with a sign-out button, the interface language, and `CanManageCultures` to decide who may add and delete languages.

Full documentation: https://github.com/kododo-dev/CultureWay
