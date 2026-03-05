using UnityEngine;
using System;
using System.Collections.Generic;
using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Base class for any MonoBehaviour that needs to consume dependencies.
    /// It automatically resolves dependencies on Awake().
    /// </summary>
    public partial class DependencyBehaviour : MonoBehaviour, IDependencyInjectionCandidate, IHasDependencyDisposable
    {
        public List<IDisposable> DependenciesDisposable { get; } = new List<IDisposable>();

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
            foreach (var d in DependenciesDisposable)
            {
                d?.Dispose();
            }
            DependenciesDisposable.Clear();
        }
    }
}