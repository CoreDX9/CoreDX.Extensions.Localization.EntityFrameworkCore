namespace CoreDX.Extensions.Localization.EntityFrameworkCore.Models;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
public class LocalizationRecord

{
    public virtual long Id { get; set; }
    public virtual string ResourceCulture { get; set; } = null!;
    public virtual string ResourceName { get; set; } = null!;
    public virtual string ContentKey { get; set; } = null!;
    public virtual string? LocalizedContent { get; set; }
}
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
