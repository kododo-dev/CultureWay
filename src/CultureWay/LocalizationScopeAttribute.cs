namespace Kododo.CultureWay;

/// <summary>
/// Sets the scope that <c>IStringLocalizer&lt;T&gt;</c> looks keys up under for the marked type:
/// <c>localizer["Pay"]</c> reads <c>{scope}.Pay</c>. Overrides the default scope, the type's short name.
/// </summary>
/// <remarks>
/// Use it to give same-named types in different namespaces their own scopes, or pass
/// <see cref="string.Empty"/> for a type that reads unscoped keys, such as a marker for shared strings.
/// A scope can itself contain dots (<c>Shop.Checkout</c>), which the editor shows as nested namespaces.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public sealed class LocalizationScopeAttribute : Attribute
{
    public LocalizationScopeAttribute(string scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        Scope = scope;
    }

    /// <summary>The scope, or <see cref="string.Empty"/> for unscoped keys.</summary>
    public string Scope { get; }
}
