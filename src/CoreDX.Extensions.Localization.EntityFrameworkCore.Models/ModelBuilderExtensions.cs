using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore.Models;

/// <summary>
/// Extensions for configure entity model of type <see cref="LocalizationRecord"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configure entity model using type <see cref="LocalizationRecord"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="configureEntity">Custom configuration action.</param>
    /// <returns>An object that can be used to configure the entity type.</returns>
    public static ModelBuilder UseLocalizationRecord(
    this ModelBuilder modelBuilder,
    Action<EntityTypeBuilder<LocalizationRecord>>? configureEntity = null) =>
        modelBuilder.UseLocalizationRecord(configureEntity);

    /// <summary>
    /// Configure entity model using custom type.
    /// </summary>
    /// <typeparam name="TLocalizationRecord">The type of entity.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="configureEntity">Custom configuration action.</param>
    /// <returns>An object that can be used to configure the entity type.</returns>
    public static ModelBuilder UseLocalizationRecord<TLocalizationRecord>(
        this ModelBuilder modelBuilder,
        Action<EntityTypeBuilder<TLocalizationRecord>>? configureEntity = null)
        where TLocalizationRecord : LocalizationRecord, new()
    {
        var entityBuilder = modelBuilder.Entity<TLocalizationRecord>();
        entityBuilder
            .HasIndex(r => new { r.ResourceName, r.ResourceCulture, r.ContentKey })
            .IsUnique();

        configureEntity?.Invoke(entityBuilder);

        return modelBuilder;
    }
}