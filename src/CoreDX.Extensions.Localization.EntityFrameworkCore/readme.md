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

// Or custom key type
public class LocalizationRecordOfIntKey : LocalizationRecord<int>
{
    public int YourProperty { get; set; }
}

```

### ServiceCollection
``` csharp
ServiceCollection services = new ();

// We use IDbContextFactoty to access database.
services.AddDbContextFactoty<YourDbContext>(options => options.UseSqlite("Localization.db"));

// If you use on .NETStandard 2.0 or 2.1.
services.AddDbContext<YourDbContext>(options => options.UseSqlite("Localization.db"));

// Add services to ServiceCollection with default entity type.
// If there are already any localization services in the service collection, no further services will be added.
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

// Then add MVC localization services.
services
    .AddMvc()
    .AddDataAnnotationsLocalization()
```

You should add EntityFrameworkCore localization services first before MVC services.

Then use `IStringLocalizer<T>` to localize your content. If `CreateLocalizationResourcesIfNotExist` is `true`, record will insert into data table auto if is first access.

If you want to clear resource cache, you can use service of type `IDynamicResourceStringLocalizerFactory`.
