using System.Resources;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

public partial class EntityFrameworkCoreStringLocalizer(
    EntityFrameworkCoreResourceManager resourceManager,
    IResourceStringProvider resourceStringProvider,
    ILogger logger
) :
    IDynamicResourceStringLocalizer
{
    private readonly ConcurrentDictionary<string, object?> _missingManifestCache = new();
    private readonly EntityFrameworkCoreResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    private readonly IResourceStringProvider _resourceStringProvider = resourceStringProvider ?? throw new ArgumentNullException(nameof(resourceStringProvider));
    private const string _searchedLocation = "";

    public EntityFrameworkCoreStringLocalizer(
        EntityFrameworkCoreResourceManager resourceManager,
        IResourceNamesCache resourceNamesCache,
        ILogger logger)
        : this(
              resourceManager,
              new EntityFrameworkCoreResourceStringProvider(resourceNamesCache, resourceManager),
              logger)
    {
        
    }

    public string ResourceName => resourceManager.ResourceName;

    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var value = GetStringSafely(name, null);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _searchedLocation);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var format = GetStringSafely(name, null);
            var value = string.Format(format ?? name, arguments);

            return new LocalizedString(name, value, resourceNotFound: format == null, searchedLocation: _searchedLocation);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

    public void ClearResourceCache(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        resourceManager.ClearResourceCache(culture);
    }

    protected virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var resourceNames = includeParentCultures
            ? GetResourceNamesFromCultureHierarchy(culture).AsEnumerable()
            : _resourceStringProvider.GetAllResourceStrings(culture, true);

        foreach (var name in resourceNames ?? Enumerable.Empty<string>())
        {
            var value = GetStringSafely(name, culture);
            yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _searchedLocation);
        }
    }

    protected string? GetStringSafely(string name, CultureInfo? culture)
    {
        ArgumentNullException.ThrowIfNull(name);

        var keyCulture = culture ?? CultureInfo.CurrentUICulture;
        var cacheKey = $"name={name}&culture={keyCulture.Name}";

        Log.SearchedLocation(logger, name, "database", keyCulture);

        if (_missingManifestCache.ContainsKey(cacheKey))
        {
            return null;
        }

        try
        {
            return _resourceManager.GetString(name, keyCulture);
        }
        catch (MissingManifestResourceException)
        {
            _missingManifestCache.TryAdd(cacheKey, null);

            return null;
        }
    }

    private HashSet<string> GetResourceNamesFromCultureHierarchy(CultureInfo startingCulture)
    {
        var currentCulture = startingCulture;
        var resourceNames = new HashSet<string>();

        while (currentCulture != currentCulture.Parent)
        {
            var cultureResourceNames = _resourceStringProvider.GetAllResourceStrings(currentCulture, false);

            if (cultureResourceNames != null)
            {
                foreach (var resourceName in cultureResourceNames)
                {
                    resourceNames.Add(resourceName);
                }
            }

            currentCulture = currentCulture.Parent;
        }

        return resourceNames;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, $"{nameof(EntityFrameworkCoreStringLocalizer)} searched for '{{Key}}' in '{{LocationSearched}}' with culture '{{Culture}}'.", EventName = "SearchedLocation")]
        public static partial void SearchedLocation(ILogger logger, string key, string locationSearched, CultureInfo culture);
    }
}
