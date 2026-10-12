using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kododo.CultureWay;

public static class CultureWayConfigurationExtensions
{
    /// <summary>
    /// Surfaces the .resx resources associated with <typeparamref name="T"/> as a read-only translation
    /// source, using the same base-name convention ASP.NET Core uses for <see cref="IStringLocalizer{T}"/>
    /// (<c>{RootNamespace}.{ResourcesPath}.{TypeName}</c> — configure <c>ResourcesPath</c> via
    /// <c>services.AddLocalization(o => o.ResourcesPath = "Resources")</c>). No custom
    /// <see cref="IReadOnlySource"/> implementation is required.
    /// </summary>
    /// <param name="configuration">The CultureWay configuration builder.</param>
    /// <param name="prefix">
    /// Prepended to every resx key as <c>{prefix}.{key}</c>. Defaults to the scope of
    /// <typeparamref name="T"/> (<see cref="CultureWayOptions.ScopeOf"/>: its short name, e.g.
    /// <c>HomeController</c>, unless <see cref="LocalizationScopeAttribute"/> says otherwise), which is where
    /// <c>IStringLocalizer&lt;T&gt;</c> looks the keys up. Pass <see cref="string.Empty"/> to use the raw resx
    /// keys unprefixed.
    /// </param>
    public static ICultureWayConfiguration UseResources<T>(this ICultureWayConfiguration configuration, string? prefix = null)
    {
        configuration.Services.AddOptions();
        configuration.Services.AddSingleton<IReadOnlySource>(sp =>
        {
            var localizationOptions = sp.GetRequiredService<IOptions<LocalizationOptions>>();
            var loggerFactory = sp.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            var factory = new ResourceManagerStringLocalizerFactory(localizationOptions, loggerFactory);
            var localizer = factory.Create(typeof(T));

            return new ResxReadOnlySource(localizer, prefix ?? configuration.Options.ScopeOf(typeof(T)), configuration.Options);
        });
        return configuration;
    }
}
