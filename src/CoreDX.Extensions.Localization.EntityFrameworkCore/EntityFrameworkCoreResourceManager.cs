using CoreDX.Extensions.Localization.EntityFrameworkCore.Models;
#if NET5_0_OR_GREATER
#else
using Microsoft.Extensions.DependencyInjection;
#endif
using Microsoft.Extensions.Options;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// A manager to manage resources using <see cref="DbContext"/>.
/// </summary>
/// <param name="resourceName">The resource name.</param>
public abstract class EntityFrameworkCoreResourceManager(string resourceName)
{
    /// <summary>
    /// Get resource name.
    /// </summary>
    public virtual string ResourceName { get; } = resourceName ?? throw new ArgumentNullException(nameof(resourceName));

    /// <summary>
    /// Get culture based string.
    /// </summary>
    /// <param name="name">The string name.</param>
    /// <param name="culture">The <see cref="CultureInfo"/>.</param>
    /// <returns></returns>
    public abstract string GetString(string name, CultureInfo? culture = null);

    /// <summary>
    /// Clear resource cache to reload.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/>.</param>
    public abstract void ClearResourceCache(CultureInfo culture);

    internal abstract ConcurrentDictionary<string, string?>? GetResourceSet(CultureInfo culture, bool tryParents);
}

#if NET5_0_OR_GREATER
/// <inheritdoc />
/// <typeparam name="TDbContext">The type of <see cref="DbContext"/> to manage resources.</typeparam>
/// <typeparam name="TLocalizationRecord">The type of localization record entity.</typeparam>
/// <param name="dbContextFactory">The factory.</param>
/// <param name="options">The options.</param>
/// <param name="resourceName">The resource name.</param>
public class EntityFrameworkCoreResourceManager<TDbContext, TLocalizationRecord>(
    IDbContextFactory<TDbContext> dbContextFactory,
#else
/// <inheritdoc />
/// <typeparam name="TDbContext">The type of <see cref="DbContext"/> to manage resources.</typeparam>
/// <typeparam name="TLocalizationRecord">The type of localization record entity.</typeparam>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="options">The options.</param>
/// <param name="resourceName">The resource name.</param>
public class EntityFrameworkCoreResourceManager<TDbContext, TLocalizationRecord>(
    IServiceProvider serviceProvider,
#endif
    IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> options,
    string resourceName

)
    : EntityFrameworkCoreResourceManager(resourceName)
    where TDbContext : DbContext
    where TLocalizationRecord : LocalizationRecord, new()
{
#if NET5_0_OR_GREATER
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
#else
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
#endif

    private readonly IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> _options = options;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string?>> _resourcesCache = new();

    /// <inheritdoc />
    public override string GetString(string name, CultureInfo? culture = null)
    {
        var localCulture = culture ?? CultureInfo.CurrentUICulture;
        var resourceSet = GetResourceSet(localCulture, tryParents: true);

        if(resourceSet?.TryGetValue(name, out var resource) is true)
        {
            return resource ?? name;
        }
        else
        {
            if (_options.CurrentValue.CreateLocalizationResourcesIfNotExist)
            {
#if NET5_0_OR_GREATER
                using var dbContext = _dbContextFactory.CreateDbContext();
#else
                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#endif


                var cultureName = localCulture.Name;
                dbContext.Set<TLocalizationRecord>().Add(new()
                {
                    ResourceName = ResourceName,
                    ResourceCulture = cultureName,
                    ContentKey = name
                });

                try
                {
                    dbContext.SaveChanges();
                }
#if DEBUG
#pragma warning disable CS0168 // 声明了变量，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量，但从未使用过
#else
                catch
#endif
                {
                    // ignore
                }
            }
            
            return name;
        }
    }

    /// <inheritdoc />
    public override void ClearResourceCache(CultureInfo culture)
    {
        if (culture is null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        var key = GetResourceCacheKey(culture);
        _resourcesCache.TryRemove(key, out var _);
    }

    internal override ConcurrentDictionary<string, string?>? GetResourceSet(CultureInfo culture, bool tryParents)
    {
        TryLoadResourceSet(culture);

        var key = GetResourceCacheKey(culture);
        if (!_resourcesCache.ContainsKey(key))
        {
            return null;
        }

        if (tryParents)
        {
            var allResources = new ConcurrentDictionary<string, string?>();
            do
            {
                if (_resourcesCache.TryGetValue(key, out var resources))
                {
                    foreach (var entry in resources)
                    {
                        allResources.TryAdd(entry.Key, entry.Value);
                    }
                }

                culture = culture.Parent;
            } while (culture != CultureInfo.InvariantCulture);

            return allResources;
        }
        else
        {
            _resourcesCache.TryGetValue(key, out var resources);

            return resources;
        }
    }

    private void TryLoadResourceSet(CultureInfo culture)
    {
        var key = GetResourceCacheKey(culture);
        if (!_resourcesCache.ContainsKey(key))
        {
#if NET5_0_OR_GREATER
                using var dbContext = _dbContextFactory.CreateDbContext();
#else
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
#endif

            var cultureName = culture.Name;
            var resources = dbContext.Set<TLocalizationRecord>()
                .Where(r => r.ResourceName == ResourceName)
                .Where(r => r.ResourceCulture == cultureName)
                .Select(r => new
                {
                    r.ContentKey,
                    r.LocalizedContent
                })
                .AsEnumerable()
                .Select(x => new KeyValuePair<string, string?>(x.ContentKey, x.LocalizedContent));

            _resourcesCache.TryAdd(key, new ConcurrentDictionary<string, string?>(resources));
        }
    }

    private string GetResourceCacheKey(CultureInfo culture)
    {
        return $"Culture={culture.Name};resourceName={ResourceName}";
    }
}