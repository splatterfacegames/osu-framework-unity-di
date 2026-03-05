using UnityEngine;
using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Handles Unity Domain Reload events to ensure static caches are cleared, preventing memory leaks and stale references between play sessions.
    /// </summary>
    internal static class DependencyBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            DependencyActivator.ClearCache();
        }
    }
}
