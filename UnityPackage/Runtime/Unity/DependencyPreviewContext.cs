using System;
using System.Collections.Generic;
using UnityEngine;

namespace OsuFramework.Unity.Allocation
{
    /// <summary>
    /// Editor-facing preview provider for prefab authoring.
    /// </summary>
    /// <remarks>
    /// This component is intentionally not part of runtime dependency activation. It gives the
    /// inspector enough information to preview which dependencies a prefab would receive when
    /// placed under a real <see cref="DependencyNodeBehaviour"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DependencyPreviewContext : MonoBehaviour
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private UnityEngine.Object value;
            [SerializeField] private string contractType;
            [SerializeField] private string name;

            public UnityEngine.Object Value => value;
            public string Name => string.IsNullOrWhiteSpace(name) ? null : name;

            public Type ContractType
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(contractType))
                        return Type.GetType(contractType);

                    return value != null ? value.GetType() : null;
                }
            }
        }
    }
}
