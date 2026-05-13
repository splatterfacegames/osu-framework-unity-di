# osu!framework DI for Unity

This package is a port of the Dependency Injection (DI) system from [osu!framework](https://github.com/ppy/osu-framework) to Unity. It provides an attribute-based workflow that maps naturally to the Unity `Transform` hierarchy while maintaining zero-allocation performance via C# Source Generators.

## The Core Concept

In Unity, Dependency Injection usually falls into two camps: reflection-based containers (Zenject) which are flexible but slow, or constructor-based containers (VContainer) which are fast but clash with `MonoBehaviour`.

This framework uses a **hierarchy-walking** approach. Dependencies are provided by a parent and consumed by children. Because resolution happens during `Awake`, it works natively with `Object.Instantiate` without requiring custom wrappers or manual injection calls.

## Key Features

- **Source Generated:** A Roslyn Source Generator analyzes your classes at compile-time and generates optimized injection code. This eliminates runtime reflection and GC allocations during dependency resolution.
- **Hierarchy-Aware:** Scoping is defined by your scene's `Transform` structure. Child objects automatically look up the tree to find the nearest provider for a requested type.
- **R3 Integration:** Native support for reactive state. `[Resolved]` properties can be `ReactiveProperty<T>`, which are automatically rebound and disposed of when the object is destroyed.
- **Async Friendly:** Inherits the `[BackgroundDependencyLoader]` pattern, allowing for safe, multi-threaded initialization.
- **Unity Authoring Friendly:** Dependencies are still declared in code, but the intended workflow is inspectable: providers live on parent objects, consumers declare required members, and prefab preview contexts can supply mock providers for editor validation.

## Performance Comparison

| Feature | Zenject | VContainer | **osu! DI** |
| :--- | :---: | :---: | :---: |
| **Resolution Type** | Runtime Reflection | Source Generated | **Source Generated** |
| **Injection Style** | Attribute / Method | Constructor | **Attribute / Method** |
| **GC Allocations** | High | Minimal | **Zero (at runtime)** |
| **Hierarchy Aware** | Yes | No | **Yes** |
| **Native Instantiate** | No (requires wrapper) | No (requires wrapper) | **Yes** |

## Installation

### 1. Install R3 (Prerequisite)
This package requires [R3](https://github.com/Cysharp/R3). Follow these two steps:

**Step A: Install R3 via NuGet**
1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).
2. Open `NuGet -> Manage NuGet Packages`, search for **"R3"**, and press **Install**.
3. Go to `Edit -> Project Settings -> Player -> Other Settings` and ensure **API Compatibility Level** is set to `.NET Standard 2.1`.
4. *Note: If you encounter version conflict errors, uncheck **"Assembly Version Validation"** in the same settings window.*

**Step B: Install R3.Unity via Git URL**
In the Unity Package Manager, click the `+` icon, select `Add package from git URL...`, and enter:
`https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity`

### 2. Install this Package
In the Unity Package Manager, click the `+` icon and select `Add package from git URL...`:
`https://github.com/YOUR_USERNAME/osu-framework-unity-di.git?path=/UnityPackage`

---

## Usage Guide

### 1. Providing Dependencies
Inherit from `DependencyNodeBehaviour` to provide dependencies to children. Use the `[Cached]` attribute on fields or properties.

```csharp
// Note: Classes using DI must be 'partial' for the Source Generator
public partial class GameController : DependencyNodeBehaviour
{
    [Cached]
    private string version = "1.0.0";

    [Cached]
    public ReactiveProperty<int> GlobalScore { get; } = new ReactiveProperty<int>(0);
}
```

### 2. Consuming Dependencies
Inherit from `DependencyBehaviour` and use `[Resolved]` for property injection, or `[BackgroundDependencyLoader]` for method injection.

```csharp
public partial class ScoreDisplay : DependencyBehaviour
{
    // Resolved properties must be private or protected with a setter
    [Resolved]
    protected ReadOnlyReactiveProperty<int> Score { get; private set; }

    // Method injection: parameters are resolved from the hierarchy
    [BackgroundDependencyLoader]
    private void load(string version)
    {
        Debug.Log($"Initialized version: {version}");
        
        // Use 'DependenciesDisposable' to track subscriptions for automatic cleanup
        Score.Subscribe(v => Debug.Log($"Score: {v}")).AddTo(DependenciesDisposable);
    }
}
```

### 3. Hierarchy Overrides
Dependencies are resolved by walking up the tree. You can override a dependency at any level by caching a new value of the same type in a child `DependencyNodeBehaviour`.

```csharp
public partial class SubMenu : DependencyNodeBehaviour
{
    // This string will be provided to all children of SubMenu, 
    // overriding any string cached by parent nodes.
    [Cached]
    private string localContext = "SubMenuContext";
}
```

### 4. Ambient Dependencies vs Explicit Init Args

Use `[Cached]` / `[Resolved]` for ambient services that belong to a hierarchy scope:

- theme, audio, input, localization
- save systems and profile services
- reactive score, health, inventory, or settings streams
- screen navigation and factories

Use explicit initialization for required per-instance values that the caller must choose:

- route arguments
- selected item id
- save slot
- initial tab
- player index

Those values should be passed through a clear `Init(...)` method or an object-creation helper instead of being promoted into static state or a global service. Implement `IInitializable<T>` and use `InstantiateWithArguments` / `AddComponentWithArguments` when the value must exist before `Awake`.

```csharp
public partial class InventoryScreen : DependencyBehaviour, IInitializable<InventoryRouteArgs>
{
    private InventoryRouteArgs args;

    public void Init(InventoryRouteArgs args)
    {
        this.args = args;
    }

    [Resolved]
    private IAudioService audio { get; set; }
}
```

The result is the same split you would expect from constructor-style Unity DI: the hierarchy supplies ambient context, while the caller supplies values that make this one instance unique.

## Inspector Workflow

Editor tooling should make the dependency graph visible without entering Play Mode:

- selected providers show their `[Cached]` members
- selected consumers show their `[Resolved]` members and loader parameters
- each resolved dependency reports its source object: self, parent, ancestor, or missing
- missing dependencies produce validation warnings
- prefabs can be inspected under a `DependencyPreviewContext` that supplies mock providers

This keeps the code-first osu!framework DI model compatible with Unity's normal scene and prefab workflow.

## How it Works

1. **Source Generation:** At compile-time, the generator creates a partial class for your behaviour that implements `ISourceGeneratedDependencyActivator`.
2. **Awake Hook:** `DependencyBehaviour.Awake` is called.
3. **Hierarchy Walk:** It calls `transform.GetParentNode()`, walking up `transform.parent` until it finds an `IDependencyNode`.
4. **Resolution:** The generated code fetches the required types from the parent's `DependencyContainer` and assigns them to your fields.
5. **R3 Rebinding:** If the type is an `ReactiveProperty`, the system creates a two-way bound copy and adds it to a `CompositeDisposable`, which is cleared in `OnDestroy`.

## Attribution & License

Licensed under the **MIT License**.

This project is a port of the `osu.Framework.Allocation` system.
Original architecture and source generation by **ppy Pty Ltd**.
Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>. See [LICENSE](./LICENSE) for full details.
