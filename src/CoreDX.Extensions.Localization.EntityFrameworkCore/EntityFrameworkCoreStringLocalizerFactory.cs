using CoreDX.Extensions.Localization.EntityFrameworkCore.Models;
#if NET5_0_OR_GREATER

#else
using Microsoft.Extensions.DependencyInjection;
#endif
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// An <see cref="IStringLocalizerFactory"/> that creates instances of <see cref="EntityFrameworkCoreStringLocalizerFactory{TDbContext, TLocalizationRecord}"/>.
/// </summary>
public class EntityFrameworkCoreStringLocalizerFactory<TDbContext, TLocalizationRecord> : IDynamicResourceStringLocalizerFactory
    where TDbContext : DbContext
    where TLocalizationRecord : LocalizationRecord, new()
{
    private readonly IResourceNamesCache _resourceNamesCache = new ResourceNamesCache();
    private readonly ConcurrentDictionary<string, IDynamicResourceStringLocalizer> _localizerCache = new();
    private readonly IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> _localizationOptions;
#if NET5_0_OR_GREATER
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
#else
    private readonly IServiceProvider _serviceProvider;
#endif

    private readonly ILoggerFactory _loggerFactory;


#if NET5_0_OR_GREATER
    /// <summary>
    /// Creates a new <see cref="EntityFrameworkCoreStringLocalizerFactory{TDbContext, TLocalizationRecord}"/>.
    /// </summary>
    /// <param name="localizationOptions">The <see cref="IOptionsMonitor{LocalizationOptions}"/>.</param>
    /// <param name="dbContextFactory">The <see cref="IDbContextFactory{TContext}"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public EntityFrameworkCoreStringLocalizerFactory(
#else
    /// <summary>
    /// Creates a new <see cref="EntityFrameworkCoreStringLocalizerFactory{TDbContext, TLocalizationRecord}"/>.
    /// </summary>
    /// <param name="localizationOptions">The <see cref="IOptionsMonitor{LocalizationOptions}"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public EntityFrameworkCoreStringLocalizerFactory(
#endif
        IOptionsMonitor<EntityFrameworkCoreLocalizationOptions> localizationOptions,
#if NET5_0_OR_GREATER
        IDbContextFactory<TDbContext> dbContextFactory,
#else
        IServiceProvider serviceProvider,
#endif
        ILoggerFactory loggerFactory)
    {
        _localizationOptions = localizationOptions ?? throw new ArgumentNullException(nameof(localizationOptions));
#if NET5_0_OR_GREATER
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
#else
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
#endif
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Creates a <see cref="EntityFrameworkCoreStringLocalizer"/> using the <see cref="Assembly"/> and
    /// <see cref="Type.FullName"/> of the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="resourceSource">The <see cref="Type"/>.</param>
    /// <returns>The <see cref="EntityFrameworkCoreStringLocalizer"/>.</returns>
    public IStringLocalizer Create(Type resourceSource)
    {
        if (resourceSource is null)
        {
            throw new ArgumentNullException(nameof(resourceSource));
        }

        // Get without Add to prevent unnecessary lambda allocation
        if (!_localizerCache.TryGetValue(resourceSource.AssemblyQualifiedName!, out var localizer))
        {
            var typeInfo = resourceSource.GetTypeInfo();
            var baseName = GetResourcePrefix(typeInfo);

            localizer = CreateEntityFrameworkCoreStringLocalizer(baseName);

            _localizerCache[resourceSource.AssemblyQualifiedName!] = localizer;
        }

        return localizer;
    }

    /// <summary>
    /// Creates a <see cref="EntityFrameworkCoreStringLocalizer"/>.
    /// </summary>
    /// <param name="baseName">The base name of the resource to load strings from.</param>
    /// <param name="location">The location to load resources from.</param>
    /// <returns>The <see cref="EntityFrameworkCoreStringLocalizer"/>.</returns>
    public IStringLocalizer Create(string baseName, string location)
    {
        if (string.IsNullOrEmpty(baseName))
        {
            throw new ArgumentException($@"""{nameof(baseName)}"" can not be null or empty.", nameof(baseName));
        }

        if (string.IsNullOrEmpty(location))
        {
            throw new ArgumentException($@"""{nameof(location)}"" can not be null or empty.", nameof(location));
        }

        return _localizerCache.GetOrAdd(GetNameBasedLocalizerCacheKey(baseName, location), _ =>
        {
            var assemblyName = new AssemblyName(location);
            var assembly = Assembly.Load(assemblyName);
            baseName = GetResourcePrefix(baseName, location);

            return CreateEntityFrameworkCoreStringLocalizer(baseName);
        });
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public void ClearResourceCache(Type resourceSource, CultureInfo culture)
    {
        if (resourceSource is null)
        {
            throw new ArgumentNullException(nameof(resourceSource));
        }

        if (culture is null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        if (_localizerCache.TryGetValue(resourceSource.AssemblyQualifiedName!, out var localizer))
        {
            localizer.ClearResourceCache(culture);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void ClearResourceCache(string baseName, string location, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(baseName))
        {
            throw new ArgumentException($@"""{nameof(baseName)}"" can not be null or empty.", nameof(baseName));
        }

        if (string.IsNullOrEmpty(location))
        {
            throw new ArgumentException($@"""{nameof(location)}"" can not be null or empty.", nameof(location));
        }

        if (culture is null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        if (_localizerCache.TryGetValue(GetNameBasedLocalizerCacheKey(baseName, location), out var localizer))
        {
            localizer.ClearResourceCache(culture);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void ClearResourceCache(string resourceName, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            throw new ArgumentException($@"""{nameof(resourceName)}"" can not be null or empty.", nameof(resourceName));
        }

        if (culture is null)
        {
            throw new ArgumentNullException(nameof(culture));
        }

        if (_localizerCache.Any(c => c.Value.ResourceName == resourceName))
        {
            _localizerCache.First(c => c.Value.ResourceName == resourceName).Value.ClearResourceCache(culture);
        }
    }

    /// <summary>Creates a <see cref="EntityFrameworkCoreStringLocalizer"/> for the given input.</summary>
    /// <param name="baseName">The base name of the resource to search for.</param>
    /// <returns>A <see cref="EntityFrameworkCoreStringLocalizer"/> for the given <paramref name="baseName"/>.</returns>
    /// <remarks>This method is virtual for testing purposes only.</remarks>
    protected virtual EntityFrameworkCoreStringLocalizer CreateEntityFrameworkCoreStringLocalizer(
        string baseName)
    {
        return new EntityFrameworkCoreStringLocalizer(
            new EntityFrameworkCoreResourceManager<TDbContext, TLocalizationRecord>(
#if NET5_0_OR_GREATER
                _dbContextFactory,
#else
                _serviceProvider,
#endif
                _localizationOptions,
                baseName),
            _resourceNamesCache,
            _loggerFactory.CreateLogger<EntityFrameworkCoreStringLocalizer>());
    }

    /// <summary>
    /// Gets the resource prefix used to look up the resource.
    /// </summary>
    /// <param name="typeInfo">The type of the resource to be looked up.</param>
    /// <returns>The prefix for resource lookup.</returns>
    protected virtual string GetResourcePrefix(TypeInfo typeInfo)
    {
        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return GetResourcePrefix(
            typeInfo,
#if NETSTANDARD2_0
            new AssemblyName(typeInfo.Assembly.FullName).Name,   
#else
            GetRootNamespace(typeInfo.Assembly),
#endif
            GetResourcePath(typeInfo.Assembly));
    }

    /// <summary>
    /// Gets the resource prefix used to look up the resource.
    /// </summary>
    /// <param name="typeInfo">The type of the resource to be looked up.</param>
    /// <param name="baseNamespace">The base namespace of the application.</param>
    /// <param name="resourcesRelativePath">The folder containing all resources.</param>
    /// <returns>The prefix for resource lookup.</returns>
    /// <remarks>
    /// For the type "Sample.Controllers.Home" if there's a resourceRelativePath return
    /// "Sample.Resourcepath.Controllers.Home" if there isn't one then it would return "Sample.Controllers.Home".
    /// </remarks>
    protected virtual string GetResourcePrefix(TypeInfo typeInfo, string? baseNamespace, string? resourcesRelativePath)
    {
        if (typeInfo is null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        if (string.IsNullOrEmpty(baseNamespace))
        {
            throw new ArgumentException($@"""{nameof(baseNamespace)}"" can not be null or empty.", nameof(baseNamespace));
        }

        if (string.IsNullOrEmpty(typeInfo.FullName))
        {
            throw new ArgumentException($@"""{typeInfo.FullName}"" can not be null or empty.", typeInfo.FullName);
        }

        if (string.IsNullOrEmpty(resourcesRelativePath))
        {
            return typeInfo.FullName;
        }
        else
        {
            // This expectation is defined by dotnet's automatic resource storage.
            // We have to conform to "{RootNamespace}.{ResourceLocation}.{FullTypeName - RootNamespace}".
            return baseNamespace + "." + resourcesRelativePath + "." + TrimPrefix(typeInfo.FullName, baseNamespace + ".");
        }
    }

    /// <summary>
    /// Gets the resource prefix used to look up the resource.
    /// </summary>
    /// <param name="baseResourceName">The name of the resource to be looked up</param>
    /// <param name="baseNamespace">The base namespace of the application.</param>
    /// <returns>The prefix for resource lookup.</returns>
    protected virtual string GetResourcePrefix(string baseResourceName, string baseNamespace)
    {
        if (string.IsNullOrEmpty(baseResourceName))
        {
            throw new ArgumentException($@"""{nameof(baseResourceName)}"" can not be null or empty.", nameof(baseResourceName));
        }

        if (string.IsNullOrEmpty(baseNamespace))
        {
            throw new ArgumentException($@"""{nameof(baseNamespace)}"" can not be null or empty.", nameof(baseNamespace));
        }

        var assemblyName = new AssemblyName(baseNamespace);
        var assembly = Assembly.Load(assemblyName);
#if NETSTANDARD2_0
        string resourcePath = GetResourcePath(assembly);
        baseResourceName = string.Concat(baseNamespace + "." + resourcePath, TrimPrefix(baseResourceName, baseNamespace + "."));
#else
        var rootNamespace = GetRootNamespace(assembly);
        var resourceLocation = GetResourcePath(assembly);
        var locationPath = rootNamespace + "." + resourceLocation;
        baseResourceName = locationPath + "." + TrimPrefix(baseResourceName, baseNamespace + ".");
#endif

        return baseResourceName;
    }

#if NETSTANDARD2_0
#else
    /// <summary>Gets a <see cref="RootNamespaceAttribute"/> from the provided <see cref="Assembly"/>.</summary>
    /// <param name="assembly">The assembly to get a <see cref="RootNamespaceAttribute"/> from.</param>
    /// <returns>The <see cref="RootNamespaceAttribute"/> associated with the given <see cref="Assembly"/>.</returns>
    /// <remarks>This method is protected and virtual for testing purposes only.</remarks>
    protected virtual RootNamespaceAttribute? GetRootNamespaceAttribute(Assembly assembly)
    {
        return assembly.GetCustomAttribute<RootNamespaceAttribute>();
    }
#endif

    /// <summary>Gets a <see cref="ResourceLocationAttribute"/> from the provided <see cref="Assembly"/>.</summary>
    /// <param name="assembly">The assembly to get a <see cref="ResourceLocationAttribute"/> from.</param>
    /// <returns>The <see cref="ResourceLocationAttribute"/> associated with the given <see cref="Assembly"/>.</returns>
    /// <remarks>This method is protected and virtual for testing purposes only.</remarks>
    protected virtual ResourceLocationAttribute? GetResourceLocationAttribute(Assembly assembly)
    {
        return assembly.GetCustomAttribute<ResourceLocationAttribute>();
    }

    private string GetResourcePath(Assembly assembly)
    {
        var resourceLocationAttribute = GetResourceLocationAttribute(assembly);

        // If we don't have an attribute assume all assemblies use the same resource location.
        var resourceLocation = resourceLocationAttribute == null
            ? _localizationOptions.CurrentValue.ResourcesPath ?? string.Empty
            : resourceLocationAttribute.ResourceLocation + ".";
        resourceLocation = resourceLocation
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        return resourceLocation;
    }

#if NETSTANDARD2_0
#else
    private string? GetRootNamespace(Assembly assembly)
    {
        var rootNamespaceAttribute = GetRootNamespaceAttribute(assembly);

        if (rootNamespaceAttribute != null)
        {
            return rootNamespaceAttribute.RootNamespace;
        }

        return assembly.GetName().Name;
    }
#endif

    private static string? TrimPrefix(string name, string prefix)
    {
        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            return name.Substring(prefix.Length);
        }

        return name;
    }

    private static string GetNameBasedLocalizerCacheKey(string baseName, string location)
    {
        return $"B={baseName},L={location}";
    }
}
