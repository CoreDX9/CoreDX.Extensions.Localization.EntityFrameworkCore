namespace CoreDX.Extensions.Localization.EntityFrameworkCore
{
    /// <summary>
    /// A provider to get resource strings.
    /// </summary>
    public interface IResourceStringProvider
    {
        /// <summary>
        /// Get all resource strings.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/>.</param>
        /// <param name="throwOnMissing">Throw exception on missing resouce or not.</param>
        /// <returns>The set of resource strings.</returns>
        IList<string>? GetAllResourceStrings(CultureInfo culture, bool throwOnMissing);
    }
}