namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// Represents a factory that creates <see cref="IDynamicResourceStringLocalizerFactory"/> instances.
/// </summary>
public interface IDynamicResourceStringLocalizerFactory : IStringLocalizerFactory
{
    /// <summary>
    /// Clear resource cache.
    /// </summary>
    /// <param name="resourceSource">The <see cref="Type"/> of resource.</param>
    /// <param name="culture">The <see cref="CultureInfo"/>.</param>
    void ClearResourceCache(Type resourceSource, CultureInfo culture);

    /// <summary>
    /// Clear resource cache.
    /// </summary>
    /// <param name="baseName">The base name.</param>
    /// <param name="location">The location.</param>
    /// <param name="culture">The <see cref="CultureInfo"/>.</param>
    void ClearResourceCache(string baseName, string location, CultureInfo culture);

    /// <summary>
    /// Clear resource cache.
    /// </summary>
    /// <param name="resourceName">The resource name.</param>
    /// <param name="culture">the <see cref="CultureInfo"/>.</param>
    void ClearResourceCache(string resourceName, CultureInfo culture);
}
