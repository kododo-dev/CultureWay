# Kododo.CultureWay.PostgreSQL

PostgreSQL persistence store for CultureWay.

Retains localized strings, runtime-added languages and the default language across application restarts. Tables are created automatically in the `cultureway` schema on `InitializeCultureWayAsync()`.

## Usage

```csharp
builder.Services.AddCultureWay(x =>
{
    x.UsePostgreSQL("Host=localhost;Database=myapp;Username=postgres;Password=secret");
    // or resolve the connection string lazily:
    // x.UsePostgreSQL(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Db")!);
});

var app = builder.Build();
await app.InitializeCultureWayAsync();
```

Full documentation: https://github.com/kododo-dev/CultureWay
