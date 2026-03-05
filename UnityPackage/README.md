# osu!framework DI for Unity

Welcome to the ultimate Dependency Injection (DI) framework for Unity, ported directly from the highly performant [osu!framework](https://github.com/ppy/osu-framework).

This package provides the "Holy Grail" of DI:
- **Zero-Allocation & Lightning Fast:** Uses C# Source Generators (Roslyn Analyzers) to resolve dependencies at compile-time, completely bypassing the massive runtime overhead and GC allocations of reflection.
- **Hierarchy-Aware Scoping:** Scopes and provides dependencies organically through the Unity `Transform` hierarchy (like Extenject/Zenject), rather than forcing rigid constructor injection (like VContainer).
- **Frictionless Dynamic Instantiation:** Because it resolves via the `Transform` parent chain during `Awake()`, you can `Instantiate()` prefabs natively without needing heavy `DiContainer.InstantiatePrefab()` wrappers.
- **UniRx Reactive State:** Built-in seamless support for `IReactiveProperty<T>`.

## Why choose this over...

### Extenject / Zenject?
Extenject relies heavily on `System.Reflection` at runtime, causing massive frame drops when instantiating complex prefabs. It also generates significant garbage. **This package uses compile-time Source Generators, resulting in zero reflection and 0 bytes of GC allocation during resolution.**

### VContainer?
VContainer achieves high performance by enforcing rigid Constructor Injection. However, Unity `MonoBehaviour`s do not support constructors. VContainer forces you to split your logic into POCOs or use awkward `[Inject]` property workarounds that break the natural flow of dynamic prefab instantiation. **This package embraces `MonoBehaviour` and the `Transform` hierarchy organically.**

## Installation

This package requires **Unity 2021.3+** (.NET Standard 2.1).

### Package Manager
1. Open the Unity Package Manager (`Window -> Package Manager`).
2. Click the `+` icon and select `Add package from git URL...`.
3. Paste the following URL:
   `https://github.com/YOUR_USERNAME/osu-framework-unity-di.git?path=/UnityPackage`

*(Make sure you have UniRx installed in your project as well, as it is a required dependency.)*

## Quick Start Guide

### 1. Providing Dependencies (`[Cached]`)
To provide dependencies to child objects, inherit from `DependencyNodeBehaviour` and mark your fields or properties with `[Cached]`.

```csharp
using UnityEngine;
using UniRx;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

public partial class GameController : DependencyNodeBehaviour
{
    // Caches a standard value
    [Cached]
    private string gameVersion = "1.0.0";

    // Caches a UniRx reactive property that children can observe or modify
    [Cached]
    public IReactiveProperty<int> Score { get; private set; } = new ReactiveProperty<int>(0);
}
```

### 2. Consuming Dependencies (`[Resolved]` & `[BackgroundDependencyLoader]`)
To consume dependencies, inherit from `DependencyBehaviour` and place it on a GameObject that is a child of the `DependencyNodeBehaviour`. 

Dependencies can be injected directly into properties using `[Resolved]`, or via an initialization method marked with `[BackgroundDependencyLoader]`.

```csharp
using UnityEngine;
using UniRx;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

public partial class ScoreDisplay : DependencyBehaviour
{
    // Automatically populated via the parent's [Cached] fields
    [Resolved]
    public IReactiveProperty<int> Score { get; private set; }

    // Safe initialization method called automatically by the DI system during Awake()
    [BackgroundDependencyLoader]
    private void load(string version)
    {
        Debug.Log($"Loaded Game Version: {version}");
        
        // The subscription is automatically tied to the MonoBehaviour's lifecycle 
        // via the internal DependenciesDisposable list!
        Score.Subscribe(newScore => Debug.Log($"Score updated: {newScore}"))
             .AddTo(DependenciesDisposable);
    }
}
```

### 3. Dynamic Instantiation
Because dependencies are resolved via the `Transform` hierarchy during `Awake()`, dynamic instantiation works natively.

```csharp
// Just pass the parent Transform! 
// The DI system will walk up the chain, find the DependencyNode, and inject everything instantly.
Instantiate(scoreDisplayPrefab, parentNodeTransform);
```
