using UnityEngine;
using UniRx;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

namespace OsuFramework.Unity.Allocation.Samples
{
    /// <summary>
    /// Attach this to a prefab.
    /// </summary>
    public partial class ScoreDisplay : DependencyBehaviour
    {
        [Resolved]
        private IReactiveProperty<int> globalScore { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            globalScore.Subscribe(score => 
            {
                Debug.Log($"ScoreDisplay Prefab sees new score: {score}");
            }).AddTo(DependenciesDisposable);
        }
    }
}
