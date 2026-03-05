using UnityEngine;

namespace OsuFramework.Unity.Allocation
{
    public static class DependencyExtensions
    {
        /// <summary>
        /// Walks up the Transform hierarchy to find the nearest parent IDependencyNode.
        /// </summary>
        public static IDependencyNode GetParentNode(this Transform t)
        {
            if (t == null) return null;
            
            Transform current = t.parent;
            while (current != null)
            {
                if (current.TryGetComponent<IDependencyNode>(out var node))
                    return node;
                current = current.parent;
            }
            
            return null;
        }
    }
}
