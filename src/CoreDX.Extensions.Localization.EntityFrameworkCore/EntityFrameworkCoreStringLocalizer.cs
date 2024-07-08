using System.Resources;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// An <see cref="IStringLocalizer" /> that uses the <see cref="EntityFrameworkCoreResourceManager" /> to provide localized strings.
/// </summary>
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

    /// <summary>
    /// Create an instance of <see cref="EntityFrameworkCoreStringLocalizer"/>.
    /// </summary>
    /// <param name="resourceManager">The manager.</param>
    /// <param name="resourceNamesCache">The cache.</param>
    /// <param name="logger">The logger.</param>
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

    /// <inheritdoc />
    public string ResourceName => resourceManager.ResourceName;

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            var value = GetStringSafely(name, null);

            return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _searchedLocation);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            var format = GetStringSafely(name, null);
            var value = string.Format(format ?? name, arguments);

            return new LocalizedString(name, value, resourceNotFound: format == null, searchedLocation: _searchedLocation);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

    /// <inheritdoc />
    public void ClearResourceCache(CultureInfo culture)
    {
        if (culture is null) throw new ArgumentNullException(nameof(culture));

        resourceManager.ClearResourceCache(culture);
    }

    /// <summary>
    /// Get all strings with culture.
    /// </summary>
    /// <param name="includeParentCultures">Include parent cultures or not.</param>
    /// <param name="culture">The culture.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
    {
        if (culture is null) throw new ArgumentNullException(nameof(culture));

        var resourceNames = includeParentCultures
            ? GetResourceNamesFromCultureHierarchy(culture).AsEnumerable()
            : _resourceStringProvider.GetAllResourceStrings(culture, true);

        foreach (var name in resourceNames ?? Enumerable.Empty<string>())
        {
            var value = GetStringSafely(name, culture);
            yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _searchedLocation);
        }
    }

    /// <summary>
    /// Gets a resource string from the <see cref="EntityFrameworkCoreStringLocalizer._resourceManager" /> and returns <c>null</c> instead of
    /// throwing exceptions if a match isn't found.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="culture">The <see cref="T:System.Globalization.CultureInfo" /> to get the string for.</param>
    /// <returns>The resource string, or <c>null</c> if none was found.</returns>
    protected string? GetStringSafely(string name, CultureInfo? culture)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

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

#if NET5_0_OR_GREATER
#else
    /// <inheritdoc />
    public IStringLocalizer WithCulture(CultureInfo culture)
    {
        return this;
    }
#endif
    private static partial class Log
    {

#if NET6_0_OR_GREATER
        [LoggerMessage(1, LogLevel.Debug, $"{nameof(EntityFrameworkCoreStringLocalizer)} searched for '{{Key}}' in '{{LocationSearched}}' with culture '{{Culture}}'.", EventName = "SearchedLocation")]
        public static partial void SearchedLocation(ILogger logger, string key, string locationSearched, CultureInfo culture);
#else
        public static void SearchedLocation(ILogger logger, string key, string locationSearched, CultureInfo culture)
        {
            logger.LogDebug(
                eventId: new(1, "SearchedLocation"),
                $"{nameof(EntityFrameworkCoreStringLocalizer)} searched for '{{Key}}' in '{{LocationSearched}}' with culture '{{Culture}}'.", key,
                locationSearched,
                culture);
        }
#endif
    }
}
