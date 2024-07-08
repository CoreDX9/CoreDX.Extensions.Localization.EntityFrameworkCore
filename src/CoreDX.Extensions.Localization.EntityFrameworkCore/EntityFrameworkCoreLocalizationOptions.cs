namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// Provides programmatic configuration for EntityFrameworkCore localization.
/// </summary>
public class EntityFrameworkCoreLocalizationOptions : LocalizationOptions
{
    /// <summary>
    /// Get or set if localization record is not exist, insert data row automatic or not.
    /// </summary>
    public bool CreateLocalizationResourcesIfNotExist { get; set; }
}
