using UnityEngine;
using UniRx;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

namespace OsuFramework.Unity.Allocation.Samples
{
    /// <summary>
    /// Attach this to a root GameObject in your scene (e.g., a Canvas).
    /// </summary>
    public partial class GameRoot : DependencyNodeBehaviour
    {
        [Cached]
        public IReactiveProperty<int> GlobalScore { get; private set; } = new ReactiveProperty<int>(0);

        public GameObject scoreDisplayPrefab;

        protected override void Awake()
        {
            base.Awake();
            
            // Increment score every second to demonstrate reactivity
            Observable.Interval(System.TimeSpan.FromSeconds(1))
                .Subscribe(_ => GlobalScore.Value++)
                .AddTo(DependenciesDisposable);

            // Dynamically instantiate the prefab after 2 seconds to prove dynamic DI works
            Observable.Timer(System.TimeSpan.FromSeconds(2))
                .Subscribe(_ => 
                {
                    var instance = Instantiate(scoreDisplayPrefab, transform);
                    Debug.Log("Instantiated Score Display dynamically! It should immediately catch up to the current score.");
                })
                .AddTo(DependenciesDisposable);
        }
    }
}
