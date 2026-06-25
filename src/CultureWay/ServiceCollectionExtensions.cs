using Kododo.CultureWay.Core.Store;
using Kododo.CultureWay.Localization;
using Kododo.CultureWay.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Kododo.CultureWay;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCultureWay(this IServiceCollection services, Action<ICultureWayConfiguration> configure)
    {
        var configuration = new CultureWayConfiguration(services);
        configure(configuration);

        services.AddSingleton(configuration.Options);
        services.TryAddSingleton<IStore, InMemoryStore>();
        services.AddSingleton<TranslationCache>();
        services.AddSingleton<IStringLocalizerFactory, CultureWayStringLocalizerFactory>();
        services.AddTransient(typeof(IStringLocalizer<>), typeof(CultureWayStringLocalizer<>));

        return services;
    }
}
