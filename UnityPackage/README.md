# osu!framework DI for Unity

*The ultimate zero-allocation, hierarchy-aware Dependency Injection framework for Unity.*

**osu-framework-unity-di** is a direct port of the battle-tested dependency injection system from [ppy/osu-framework](https://github.com/ppy/osu-framework) into the Unity ecosystem. It provides the intuitive, hierarchy-aware workflow of **Extenject (Zenject)**, powered entirely by the zero-allocation C# Source Generators of **VContainer**.

---

## ✨ Why choose this framework?

Unity developers traditionally face a dilemma when choosing a DI framework:

- **Extenject / Zenject:** Offers an incredible developer experience with `[Inject]` attributes and natural scoping through the Unity `Transform` hierarchy. *However*, its reliance on `System.Reflection` at runtime causes massive CPU spikes and GC allocations when instantiating complex prefabs.
- **VContainer:** Achieves lightning-fast performance using C# Source Generators. *However*, it enforces rigid Constructor Injection which fundamentally clashes with the `MonoBehaviour` lifecycle and makes dynamic prefab instantiation cumbersome.

**This package solves both problems.** 

It was originally engineered by ppy Pty Ltd to power **osu!**, a rhythm game where precise frame-pacing and zero-allocation execution are non-negotiable. By porting this exact architecture to Unity, we bring enterprise-grade, rhythm-game-proven performance to your `MonoBehaviour`s.

### 🚀 Core Features

- **Battle-Tested at 1000+ FPS:** Built for real-time applications where every microsecond matters. It is proven to run smoothly without stuttering in a high-performance environment.
- **Source-Generated Zero-Allocation:** A custom Roslyn Source Generator analyzes `[Cached]`, `[Resolved]`, and `[BackgroundDependencyLoader]` attributes at compile-time. It emits highly optimized, reflection-free proxy code that injects dependencies with **0 bytes of GC allocation**, ensuring the Garbage Collector never spikes during gameplay.
- **Organic Hierarchy Scoping:** Dependencies flow organically down the Unity `Transform` tree. A parent `DependencyNode` automatically provides dependencies to any nested child, mimicking real-world object relationships perfectly without relying on rigid global containers.
- **Frictionless Dynamic Instantiation:** Because the DI system resolves through `transform.parent` during Unity's native `Awake()` phase, you can call `Instantiate(prefab, parent)` natively. The prefab will instantly resolve all dependencies without needing a heavy wrapper like `DiContainer.InstantiatePrefab()`.
- **First-Class UniRx Support:** The internal reactive `Bindable` system from osu!framework has been completely replaced with native [UniRx](https://github.com/neuecc/UniRx) support (`IReactiveProperty<T>`), complete with automatic lifecycle-safe subscriptions.
- **Robust Two-Stage Initialization:** The system elegantly separates object construction from dependency resolution. The `[BackgroundDependencyLoader]` pattern allows objects to safely prepare their state before entering the active game loop.

---

## 📦 Installation

This package requires **Unity 2021.3+** (.NET Standard 2.1).

### Dependencies
You must have UniRx installed in your Unity project before adding this package:
- [UniRx (com.neuecc.unirx)](https://github.com/neuecc/UniRx)

### Package Manager
1. Open the Unity Package Manager (`Window -> Package Manager`).
2. Click the `+` icon and select `Add package from git URL...`.
3. Paste the following URL:
   `https://github.com/YOUR_USERNAME/osu-framework-unity-di.git?path=/UnityPackage`

---

## 📖 Quick Start Guide

### 1. Providing Dependencies (`[Cached]`)
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

### 2. Consuming Dependencies (`[Resolved]` & `[BackgroundDependencyLoader]`)
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

### 3. Dynamic Instantiation
Because dependencies are resolved via the `Transform` hierarchy during `Awake()`, dynamic instantiation works natively.

```csharp
// Just pass the parent Transform! 
// The DI system will walk up the chain, find the DependencyNode, and inject everything instantly.
Instantiate(scoreDisplayPrefab, parentNodeTransform);
```

---

## ⚖️ Attribution & License

This project is licensed under the **MIT License**.

This package is heavily based on, and contains modified source code from, the [osu!framework](https://github.com/ppy/osu-framework) created by **ppy Pty Ltd**. The incredible zero-allocation source generator and core allocation architecture are their original work.

Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>. 
See the [LICENSE](./LICENSE) file for the full text.