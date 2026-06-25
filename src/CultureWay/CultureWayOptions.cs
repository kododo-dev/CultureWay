namespace Kododo.CultureWay;

public class CultureWayOptions
{
    /// <summary>BCP 47 culture codes supported by the application, e.g. <c>["pl", "en", "de"]</c>.</summary>
    public string[] SupportedCultures { get; set; } = ["en"];

    /// <summary>The culture used as a final fallback when a key has no translation for the current culture.</summary>
    public string DefaultCulture { get; set; } = "en";
}
