using UnityEngine;
using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// A behaviour that both consumes dependencies from its parent, and provides new [Cached] dependencies to its children.
    /// </summary>
    public class DependencyNodeBehaviour : DependencyBehaviour, IDependencyNode
    {
        private IReadOnlyDependencyContainer dependencies;
        
        public IReadOnlyDependencyContainer Dependencies
        {
            get
            {
                if (dependencies == null)
                    CreateChildDependencies();
                return dependencies;
            }
        }

        protected virtual void CreateChildDependencies()
        {
            var parentNode = transform.GetParentNode();
            IReadOnlyDependencyContainer parentContainer = parentNode?.Dependencies ?? new DependencyContainer();
            
            // MergeDependencies parses [Cached] attributes on this object and builds a new container
            dependencies = DependencyActivator.MergeDependencies(this, parentContainer);
        }

        protected override void Awake()
        {
            // Ensure our dependencies are constructed so children can access them immediately
            _ = Dependencies;
            
            // We activate ourselves using the PARENT container, not our own.
            // This prevents circular resolution where a node tries to resolve its own [Cached] fields
            // before they are fully initialized.
            var parentNode = transform.GetParentNode();
            IReadOnlyDependencyContainer parentContainer = parentNode?.Dependencies ?? new DependencyContainer();
            
            DependencyActivator.Activate(this, parentContainer);
        }
    }
}
