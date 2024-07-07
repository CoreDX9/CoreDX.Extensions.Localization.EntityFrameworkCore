namespace CoreDX.Extensions.Localization.EntityFrameworkCore.Models;

public class LocalizationRecord
{
    public virtual long Id { get; set; }
    public virtual string ResourceCulture { get; set; } = null!;
    public virtual string ResourceName { get; set; } = null!;
    public virtual string ContentKey { get; set; } = null!;
    public virtual string? LocalizedContent { get; set; }
}
