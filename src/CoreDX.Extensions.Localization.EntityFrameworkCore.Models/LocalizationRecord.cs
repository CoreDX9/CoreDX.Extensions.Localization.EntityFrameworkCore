namespace CoreDX.Extensions.Localization.EntityFrameworkCore.Models;

/// <summary>
/// Base class of LocalizationRecord
/// </summary>
/// <typeparam name="TKey">Type of <see cref="Id"/> property</typeparam>
public class LocalizationRecord<TKey> where TKey : struct, IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets Id.
    /// </summary>
    public virtual TKey Id { get; set; }

    /// <summary>
    /// Gets or sets culture and language name.
    /// </summary>
    /// <remarks>eg. zh-cn, en</remarks>
    public virtual string ResourceCulture { get; set; } = null!;

    /// <summary>
    /// Gets or sets name of localization resource name.
    /// </summary>
    public virtual string ResourceName { get; set; } = null!;

    /// <summary>
    /// Gets or sets content key of localization resource.
    /// </summary>
    /// <remarks>Original content to localize.</remarks>
    public virtual string ContentKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets localized content.
    /// </summary>
    public virtual string? LocalizedContent { get; set; }
}

/// <summary>
/// Default localization record using <see cref="long"/> as Id.
/// </summary>
public class LocalizationRecord : LocalizationRecord<long>;
