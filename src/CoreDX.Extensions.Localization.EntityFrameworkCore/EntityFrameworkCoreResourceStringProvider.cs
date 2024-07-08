using System.Resources;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// A provider to get resource strings using EntityFrameworkCore.
/// </summary>
/// <param name="resourceNamesCache">The cache.</param>
/// <param name="resourceManager">The manager.</param>
public class EntityFrameworkCoreResourceStringProvider(
    IResourceNamesCache resourceNamesCache,
    EntityFrameworkCoreResourceManager resourceManager) : IResourceStringProvider
{
    /// <inheritdoc />
    public IList<string>? GetAllResourceStrings(CultureInfo culture, bool throwOnMissing)
    {
        var cacheKey = GetResourceCacheKey(culture);

        return resourceNamesCache.GetOrAdd(cacheKey, _ =>
        {
            var resourceSet = resourceManager.GetResourceSet(culture, tryParents: false);
            if (resourceSet == null)
            {
                if (throwOnMissing)
                {
                    throw new MissingManifestResourceException($"The manifest resource '{resourceManager.ResourceName}' for the culture '{culture.Name}' is missing.");
                }
                else
                {
                    return null;
                }
            }

            return resourceSet.Select(x => x.Key).ToList();
        });
    }

    private string GetResourceCacheKey(CultureInfo culture)
    {
        var resourceName = resourceManager.ResourceName;

        return $"Culture={culture.Name};resourceName={resourceName}";
    }
}
