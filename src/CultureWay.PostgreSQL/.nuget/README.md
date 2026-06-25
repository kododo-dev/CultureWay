# Kododo.CultureWay.PostgreSQL

PostgreSQL persistence store for CultureWay.

Retains localized strings across application restarts using a single auto-managed database table.

## Usage

```csharp
builder.Services.AddCultureWay(x =>
{
    x.UsePostgreSQL("Host=localhost;Database=myapp;Username=postgres;Password=secret");
});
```
