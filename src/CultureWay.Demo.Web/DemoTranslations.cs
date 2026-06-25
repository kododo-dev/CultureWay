using Kododo.CultureWay.Core.Model;

namespace Kododo.CultureWay.Demo.Web;

static class DemoTranslations
{
    public static Translation[] Seed() =>
    [
        // English
        T("App.Title",         "en", "CultureWay Demo"),
        T("App.Tagline",       "en", "Edit your translations at runtime — no restart needed."),
        T("Nav.Home",          "en", "Home"),
        T("Nav.Editor",        "en", "Translation Editor"),
        T("Hero.Heading",      "en", "Runtime Localization for ASP.NET Core"),
        T("Hero.Subheading",   "en", "CultureWay lets you edit localized strings through a beautiful web UI — changes take effect instantly, without restarting the application."),
        T("Hero.Button",       "en", "Open Translation Editor"),
        T("Feature.1.Title",   "en", "No Restart Required"),
        T("Feature.1.Desc",    "en", "Edit translations and see changes immediately across all active requests."),
        T("Feature.2.Title",   "en", "Multiple Cultures"),
        T("Feature.2.Desc",    "en", "Manage strings for as many cultures as your application supports."),
        T("Feature.3.Title",   "en", "PostgreSQL Backed"),
        T("Feature.3.Desc",    "en", "Translations are persisted in your database and survive application restarts."),
        T("Footer.Text",       "en", "CultureWay Demo · powered by Kododo.CultureWay"),

        // Polish
        T("App.Title",         "pl", "Demo CultureWay"),
        T("App.Tagline",       "pl", "Edytuj tłumaczenia w czasie rzeczywistym — bez restartu aplikacji."),
        T("Nav.Home",          "pl", "Strona główna"),
        T("Nav.Editor",        "pl", "Edytor tłumaczeń"),
        T("Hero.Heading",      "pl", "Lokalizacja w czasie rzeczywistym dla ASP.NET Core"),
        T("Hero.Subheading",   "pl", "CultureWay pozwala edytować lokalizowane ciągi przez wbudowany panel — zmiany widoczne natychmiast, bez restartu aplikacji."),
        T("Hero.Button",       "pl", "Otwórz edytor tłumaczeń"),
        T("Feature.1.Title",   "pl", "Bez restartu"),
        T("Feature.1.Desc",    "pl", "Edytuj tłumaczenia i obserwuj zmiany natychmiast we wszystkich aktywnych żądaniach."),
        T("Feature.2.Title",   "pl", "Wiele kultur"),
        T("Feature.2.Desc",    "pl", "Zarządzaj tekstami dla tylu kultur, ile wspiera Twoja aplikacja."),
        T("Feature.3.Title",   "pl", "PostgreSQL"),
        T("Feature.3.Desc",    "pl", "Tłumaczenia są przechowywane w bazie danych i przeżywają restart aplikacji."),
        T("Footer.Text",       "pl", "Demo CultureWay · zasilane przez Kododo.CultureWay"),

        // German
        T("App.Title",         "de", "CultureWay Demo"),
        T("App.Tagline",       "de", "Übersetzungen zur Laufzeit bearbeiten — kein Neustart erforderlich."),
        T("Nav.Home",          "de", "Startseite"),
        T("Nav.Editor",        "de", "Übersetzungseditor"),
        T("Hero.Heading",      "de", "Echtzeit-Lokalisierung für ASP.NET Core"),
        T("Hero.Subheading",   "de", "CultureWay ermöglicht das Bearbeiten lokalisierter Zeichenfolgen über eine integrierte Web-UI — Änderungen werden sofort sichtbar, ohne die Anwendung neu zu starten."),
        T("Hero.Button",       "de", "Übersetzungseditor öffnen"),
        T("Feature.1.Title",   "de", "Kein Neustart erforderlich"),
        T("Feature.1.Desc",    "de", "Übersetzen Sie Texte und sehen Sie die Änderungen sofort in allen aktiven Anfragen."),
        T("Feature.2.Title",   "de", "Mehrere Kulturen"),
        T("Feature.2.Desc",    "de", "Verwalten Sie Texte für beliebig viele Kulturen, die Ihre Anwendung unterstützt."),
        T("Feature.3.Title",   "de", "PostgreSQL-gesichert"),
        T("Feature.3.Desc",    "de", "Übersetzungen werden in Ihrer Datenbank gespeichert und überstehen Neustarts."),
        T("Footer.Text",       "de", "CultureWay Demo · powered by Kododo.CultureWay"),
    ];

    private static Translation T(string key, string culture, string value) => new(key, culture, value);
}
