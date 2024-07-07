## About

Application localization services and default implementation based on EntityFrameworkCore to manage localized assembly resources.

## How to Use

### YourDbContext
``` csharp
public class YourLocalizationRecord : LocalizationRecord
{
    public int YourProperty { get; set; }
}

public class YourDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add entity type to DbContext with default entity type.
        modelBuilder.UseLocalizationRecord();

        // Add entity type to DbContext with custom entity type based on LocalizationRecord.
        modelBuilder.UseLocalizationRecord<YourLocalizationRecord>(b =>
        {
            b.Property(r => r.ResourceCulture).HasMaxLength(32);
            b.ToTable($"{nameof(YourLocalizationRecord)}s");
        });
    }
}

```

### ServiceCollection
``` csharp
ServiceCollection services = new ();

// We use IDbContextFactoty to access database.
services.AddDbContextFactoty<YourDbContext>(options => options.UseSqlite("Localization.db"));

// Add services to ServiceCollection with default entity type.
services.AddEntityFrameworkCoreLocalization<YourDbContext>(options =>
{
    options.ResourcesPath = "Resources";
    options.CreateLocalizationResourcesIfNotExist = true;
});

// Add services to ServiceCollection with custom entity type based on LocalizationRecord.
services.AddEntityFrameworkCoreLocalization<YourDbContext, YourLocalizationRecord>(options =>
{
    options.ResourcesPath = "Resources";
    options.CreateLocalizationResourcesIfNotExist = true;
});
```

Then use `IStringLocalizer<T>` to localize your content. If `CreateLocalizationResourcesIfNotExist` is `true`, record will insert into data table auto if is first access.