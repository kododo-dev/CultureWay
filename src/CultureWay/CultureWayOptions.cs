using System.Reflection;

namespace Kododo.CultureWay;

public class CultureWayOptions
{
    /// <summary>BCP 47 culture codes supported by the application, e.g. <c>["pl", "en", "de"]</c>.</summary>
    public string[] SupportedCultures { get; set; } = ["en"];

    /// <summary>The culture used as a final fallback when a key has no translation for the current culture.</summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// Maps the type of an <c>IStringLocalizer&lt;T&gt;</c> to the scope its keys are looked up under:
    /// <c>localizer["Pay"]</c> reads <c>{scope}.Pay</c>, and an empty scope reads <c>Pay</c>.
    /// Defaults to <see cref="DefaultScopeOf"/>. <c>UseResources&lt;T&gt;()</c> uses the same scope as the prefix
    /// of the keys it reads from .resx files.
    /// </summary>
    public Func<Type, string> ScopeOf { get; set; } = DefaultScopeOf;

    /// <summary>
    /// Whether a scoped localizer that finds no <c>{scope}.{key}</c> also tries the unscoped <c>{key}</c>,
    /// so strings shared by many types can live without a scope. Defaults to <c>true</c>.
    /// Each culture of the fallback chain is tried with both keys before the next culture: a translation in
    /// the user's language wins over one in the default culture, even if it comes from the unscoped key.
    /// </summary>
    public bool FallBackToUnscopedKeys { get; set; } = true;

    /// <summary>
    /// The scope set by <see cref="LocalizationScopeAttribute"/>, or else the type's short name without the
    /// generic arity: <c>CheckoutPage</c> for <c>Shop.Pages.CheckoutPage</c>.
    /// </summary>
    public static string DefaultScopeOf(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var attribute = type.GetCustomAttribute<LocalizationScopeAttribute>(inherit: false);
        if (attribute is not null)
            return attribute.Scope;

        var name = type.Name;
        var arity = name.IndexOf('`');
        return arity < 0 ? name : name[..arity];
    }
}
