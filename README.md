# osu!framework DI for Unity

Welcome to the ultimate Dependency Injection (DI) framework for Unity. This project is a direct port of the highly performant, zero-allocation dependency injection system from [ppy/osu-framework](https://github.com/ppy/osu-framework).

## The "Holy Grail" of Unity DI

Unity developers traditionally have to choose between two paradigms:
1. **Extenject / Zenject:** Excellent developer experience. Scopes dependencies naturally through the Unity `Transform` hierarchy using attributes like `[Inject]`. However, it relies heavily on `System.Reflection` at runtime, which causes massive CPU spikes and GC allocations when instantiating complex prefabs.
2. **VContainer:** Incredible performance. Uses C# Source Generators to achieve zero-allocation DI at compile-time. However, it enforces rigid Constructor Injection which fundamentally clashes with the `MonoBehaviour` lifecycle. 

**osu-framework-unity-di gives you the best of both worlds.** 
It provides the intuitive, hierarchy-aware attribute injection of Extenject, powered entirely by the zero-allocation C# Source Generators of VContainer. 

### Core Features
- **Zero-Allocation & Lightning Fast:** Bypasses `System.Reflection` entirely by using a compiled Roslyn Analyzer (`osu.Framework.SourceGeneration`) to weave dependencies at compile-time.
- **Transform Hierarchy Scoping:** Dependencies flow organically down the Unity `Transform` tree. A parent `DependencyNode` provides dependencies to any nested child automatically.
- **Frictionless Dynamic Instantiation:** Because the DI system resolves through `transform.parent` during Unity's native `Awake()` phase, you can call `Instantiate(prefab, parent)` natively. The prefab will instantly and cheaply resolve all dependencies without needing a heavy wrapper like `DiContainer.InstantiatePrefab()`.
- **First-Class UniRx Support:** The internal reactive `Bindable` system from osu!framework has been entirely replaced with native [UniRx](https://github.com/neuecc/UniRx) support (`IReactiveProperty<T>`).

## Why osu!framework's DI is the Best

The [osu!framework](https://github.com/ppy/osu-framework) was built from the ground up to power **osu!**, a rhythm game where performance, frame-pacing, and zero-allocation execution are non-negotiable. 

To achieve this, ppy Pty Ltd engineered a Dependency Injection system that is a masterclass in C# architecture:
- **Battle-Tested at 1000+ FPS:** Unlike generic enterprise DI containers that assume standard web requests or UI applications, osu!framework's DI is built specifically for real-time applications where every microsecond matters. It is proven to run smoothly in an environment that demands 1000+ frames per second without stuttering.
- **Source-Generated Zero-Allocation:** ppy developed a custom Roslyn Source Generator that analyzes `[Cached]`, `[Resolved]`, and `[BackgroundDependencyLoader]` attributes at compile-time. It emits highly optimized, reflection-free proxy code that injects dependencies with **0 bytes of GC allocation**, ensuring the Garbage Collector never spikes during gameplay.
- **Organic Hierarchy Scoping:** Instead of relying on rigid, global containers or complex sub-containers defined in code, the DI organically flows through the visual hierarchy (the `Drawable` tree in osu!, mapped to the `Transform` tree in Unity). This makes scoping incredibly intuitive—a child simply inherits dependencies from its parents, mimicking real-world object relationships perfectly.
- **Robust Two-Stage Initialization:** The system elegantly separates object construction from dependency resolution. The `[BackgroundDependencyLoader]` pattern allows objects to safely prepare their state on background threads before being pushed to the main thread, making async loading a breeze.

By porting this exact architecture to Unity, we bring enterprise-grade, rhythm-game-proven performance to your `MonoBehaviour`s.

## Installation

This package requires **Unity 2021.3+** (.NET Standard 2.1).

### Dependencies
You must have UniRx installed in your Unity project before adding this package:
- [UniRx (com.neuecc.unirx)](https://github.com/neuecc/UniRx)

### Package Manager
1. Open the Unity Package Manager (`Window -> Package Manager`).
2. Click the `+` icon and select `Add package from git URL...`.
3. Paste the following URL:
   `https://github.com/YOUR_USERNAME/osu-framework-unity-di.git?path=/UnityPackage`

## Quick Start Guide

### Providing Dependencies (`[Cached]`)
Attach a `DependencyNodeBehaviour` to a root GameObject. Mark the fields or properties you want to share with children using `[Cached]`.

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

### Consuming Dependencies (`[Resolved]` & `[BackgroundDependencyLoader]`)
Attach a `DependencyBehaviour` to any child GameObject. Use `[Resolved]` for property injection, or `[BackgroundDependencyLoader]` for a DI-safe initialization method.

```csharp
using UnityEngine;
using UniRx;
using osu.Framework.Allocation;
using OsuFramework.Unity.Allocation;

public partial class ScoreDisplay : DependencyBehaviour
{
    // Automatically populated via the parent's [Cached] fields
    [Resolved]
    public IReadOnlyReactiveProperty<int> Score { get; private set; }

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

## Attribution & License

This project is licensed under the **MIT License**.

This package is heavily based on, and contains modified source code from, the [osu!framework](https://github.com/ppy/osu-framework) created by **ppy Pty Ltd**. The incredible zero-allocation source generator and core allocation architecture are their original work.

Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>. 
See the [LICENSE](./LICENSE) file for the full text.
