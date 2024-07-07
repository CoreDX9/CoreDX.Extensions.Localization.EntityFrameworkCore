using CoreDX.Extensions.Localization.EntityFrameworkCore.Models;
using Microsoft.Extensions.Options;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

public abstract class EntityFrameworkCoreResourceManager(string resourceName)
{
    public virtual string ResourceName { get; } = resourceName ?? throw new ArgumentNullException(nameof(resourceName));

    public abstract string GetString(string name, CultureInfo? culture = null);

    public abstract void ClearResourceCache(CultureInfo culture);

    internal abstract ConcurrentDictionary<string, string?>? GetResourceSet(CultureInfo culture, bool tryParents);
}

public class EntityFrameworkCoreResourceManager<TDbContext, TLocalizationRecord>(
    IDbContextFactory<TDbContext> dbContextFactory,
    IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> options,
    string resourceName
)
    : EntityFrameworkCoreResourceManager(resourceName)
    where TDbContext : DbContext
    where TLocalizationRecord : LocalizationRecord, new()
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
    private readonly IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> _options = options;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string?>> _resourcesCache = new();

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
                using var dbContext = _dbContextFactory.CreateDbContext();

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

    public override void ClearResourceCache(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

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
            using var dbContext = _dbContextFactory.CreateDbContext();

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