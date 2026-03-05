using UnityEngine;
using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Base class for any MonoBehaviour that needs to consume dependencies.
    /// It automatically resolves dependencies on Awake().
    /// </summary>
    public class DependencyBehaviour : MonoBehaviour, IDependencyInjectionCandidate
    {
        protected virtual void Awake()
        {
            // Find the nearest dependency provider in the parent hierarchy
            var parentNode = transform.GetParentNode();
            IReadOnlyDependencyContainer parentContainer = parentNode?.Dependencies ?? new DependencyContainer();
            
            // Inject dependencies into [Resolved] properties and invoke [BackgroundDependencyLoader] methods
            DependencyActivator.Activate(this, parentContainer);
        }

        protected virtual void OnDestroy()
        {
            // Clean up any potential static references or delegates if needed,
            // though standard Unity object destruction usually suffices for basic behaviours.
        }
    }
}
