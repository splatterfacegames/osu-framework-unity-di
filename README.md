# osu!framework DI for Unity

This is a port of the Dependency Injection system from [osu!framework](https://github.com/ppy/osu-framework) to Unity. It combines the hierarchy-aware workflow of Zenject with the performance of source-generated DI.

## Key Features

- **Source Generated:** Uses Roslyn Source Generators to resolve dependencies at compile-time. No runtime reflection. No GC allocations during injection.
- **Hierarchy-Aware:** Dependencies flow through the Unity `Transform` hierarchy. Child objects automatically inherit dependencies from their parents.
- **Native Instantiation:** Works with standard `Object.Instantiate`. Prefabs resolve dependencies during `Awake` by walking up the transform tree.
- **UniRx Integration:** Native support for `IReactiveProperty<T>`. Rebound properties are automatically disposed of in `OnDestroy`.
- **Performance:** Optimized for real-time applications where frame-pacing is critical.

## Comparison

| Feature | Zenject | VContainer | **osu! DI** |
| :--- | :---: | :---: | :---: |
| **Performance** | Reflection-heavy | Source Generated | **Source Generated** |
| **Workflow** | Hierarchy/Attribute | Constructor | **Hierarchy/Attribute** |
| **Allocation** | High | Low | **Zero (at runtime)** |
| **Prefab Support** | Manual Wrapper | Manual Wrapper | **Native** |

## Installation

Requires **Unity 2021.3+**.

1. Install [UniRx](https://github.com/neuecc/UniRx) in your project.
2. In the Unity Package Manager, add package from git URL:
   `https://github.com/YOUR_USERNAME/osu-framework-unity-di.git?path=/UnityPackage`

## Usage

### Providing Dependencies

Inherit from `DependencyNodeBehaviour` and use the `[Cached]` attribute.

```csharp
public partial class GameController : DependencyNodeBehaviour
{
    [Cached]
    private string version = "1.0.0";

    [Cached]
    public IReactiveProperty<int> Score { get; } = new ReactiveProperty<int>();
}
```

### Consuming Dependencies

Inherit from `DependencyBehaviour` and use `[Resolved]` or `[BackgroundDependencyLoader]`.

```csharp
public partial class ScoreDisplay : DependencyBehaviour
{
    [Resolved]
    public IReadOnlyReactiveProperty<int> Score { get; private set; }

    [BackgroundDependencyLoader]
    private void load(string version)
    {
        Debug.Log($"Version: {version}");
        Score.Subscribe(v => UpdateUI(v)).AddTo(DependenciesDisposable);
    }
}
```

## Attribution & License

Licensed under **MIT**.

Based on source code from [osu!framework](https://github.com/ppy/osu-framework) by **ppy Pty Ltd**.
Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>. See [LICENSE](./LICENSE) for details.
