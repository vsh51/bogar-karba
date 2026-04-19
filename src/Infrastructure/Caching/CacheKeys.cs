using System.Globalization;

namespace Infrastructure.Caching;

internal static class CacheKeys
{
    public static string PublishedChecklist(Guid id) =>
        string.Create(CultureInfo.InvariantCulture, $"checklist:published:{id}");
}
