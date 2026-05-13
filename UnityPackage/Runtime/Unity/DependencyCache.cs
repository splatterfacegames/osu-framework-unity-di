using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Exposes cache maintenance operations needed by Unity integration code.
    /// </summary>
    public static class DependencyCache
    {
        public static void Clear()
        {
            DependencyActivator.ClearCache();
        }
    }
}
