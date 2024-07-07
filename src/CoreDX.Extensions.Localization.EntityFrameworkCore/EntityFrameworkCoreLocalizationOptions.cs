namespace CoreDX.Extensions.Localization.EntityFrameworkCore;

/// <summary>
/// Provides programmatic configuration for EntityFrameworkCore localization.
/// </summary>
public class EntityFrameworkCoreLocalizationOptions : LocalizationOptions
{
    public bool CreateLocalizationResourcesIfNotExist { get; set; }
}
