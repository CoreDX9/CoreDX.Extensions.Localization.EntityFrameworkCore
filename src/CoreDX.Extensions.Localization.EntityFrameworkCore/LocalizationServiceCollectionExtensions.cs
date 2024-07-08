using CoreDX.Extensions.Localization.EntityFrameworkCore;
using CoreDX.Extensions.Localization.EntityFrameworkCore.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up localization services in an <see cref="IServiceCollection" />.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for application localization.
    /// </summary>
    /// <typeparam name="TDbContext">The context type use for providing localization resource.</typeparam>
    /// <typeparam name="TLocalizationRecord">The entity type use for providing localization resource.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkCoreLocalization<TDbContext, TLocalizationRecord>(this IServiceCollection services)
        where TDbContext : DbContext
        where TLocalizationRecord : LocalizationRecord, new()
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddOptions();

        AddEntityFrameworkCoreLocalizationServices<TDbContext, TLocalizationRecord>(services);

        return services;
    }

    /// <summary>
    /// Adds services required for application localization.
    /// </summary>
    /// <typeparam name="TDbContext">The context type use for providing localization resource.</typeparam>
    /// <typeparam name="TLocalizationRecord">The entity type use for providing localization resource.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{LocalizationOptions}"/> to configure the <see cref="EntityFrameworkCoreLocalizationOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddEntityFrameworkCoreLocalization<TDbContext, TLocalizationRecord>(
        this IServiceCollection services,
        Action<EntityFrameworkCoreLocalizationOptions> setupAction)
        where TDbContext : DbContext
        where TLocalizationRecord : LocalizationRecord, new()
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (setupAction is null) throw new ArgumentNullException(nameof(setupAction));

        services.AddOptions();

        AddLocalizationServices<TDbContext, TLocalizationRecord>(services, setupAction);

        return services;
    }

    // To enable unit testing
    internal static void AddEntityFrameworkCoreLocalizationServices<TDbContext, TLocalizationRecord>(IServiceCollection services)
        where TDbContext : DbContext
        where TLocalizationRecord : LocalizationRecord, new()
    {
        services.TryAddSingleton<IDynamicResourceStringLocalizerFactory, EntityFrameworkCoreStringLocalizerFactory<TDbContext, TLocalizationRecord>>();
        services.TryAddSingleton(provider => provider.GetRequiredService<IDynamicResourceStringLocalizerFactory>() as IStringLocalizerFactory);
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
    }

    internal static void AddLocalizationServices<TDbContext, TLocalizationRecord>(
        IServiceCollection services,
        Action<EntityFrameworkCoreLocalizationOptions> setupAction)
        where TDbContext : DbContext
        where TLocalizationRecord : LocalizationRecord, new()
    {
        AddEntityFrameworkCoreLocalizationServices<TDbContext, TLocalizationRecord>(services);
        services.Configure(setupAction);
    }
}