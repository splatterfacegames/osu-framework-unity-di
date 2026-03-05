using osu.Framework.Allocation;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Represents a node in the Transform hierarchy that provides dependencies to its children.
    /// </summary>
    public interface IDependencyNode
    {
        IReadOnlyDependencyContainer Dependencies { get; }
    }
}
