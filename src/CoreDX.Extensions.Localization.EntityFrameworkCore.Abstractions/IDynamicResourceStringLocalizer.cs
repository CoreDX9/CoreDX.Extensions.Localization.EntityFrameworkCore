namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// Represents a service that provides localized strings.
/// </summary>
public interface IDynamicResourceStringLocalizer : IStringLocalizer
{
    /// <summary>
    /// Get the name of resource.
    /// </summary>
    string ResourceName { get; }

    /// <summary>
    /// Clear resource cache of <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/>.</param>
    void ClearResourceCache(CultureInfo culture);
}
