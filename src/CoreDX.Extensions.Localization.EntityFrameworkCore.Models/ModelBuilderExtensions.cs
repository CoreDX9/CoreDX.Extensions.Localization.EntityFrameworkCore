using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace CoreDX.Extensions.Localization.EntityFrameworkCore.Models;

public static class ModelBuilderExtensions
{
    public static ModelBuilder UseLocalizationRecord(
    this ModelBuilder modelBuilder,
    Action<EntityTypeBuilder<LocalizationRecord>>? configureEntity = null) =>
        modelBuilder.UseLocalizationRecord(configureEntity);

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