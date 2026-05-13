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

## Performance Comparison

| Feature | Zenject | VContainer | **osu! DI** |
| :--- | :---: | :---: | :---: |
| **Resolution Type** | Runtime Reflection | Source Generated | **Source Generated** |
| **Injection Style** | Attribute / Method | Constructor | **Attribute / Method** |
| **GC Allocations** | High | Minimal | **Zero (at runtime)** |
| **Hierarchy Aware** | Yes | No | **Yes** |
| **Native Instantiate** | No (requires wrapper) | No (requires wrapper) | **Yes** |

## Package Distribution

This repository is not currently using CI/CD to build or publish UPM packages. There are no root GitHub Actions or release workflows in this checkout. The core package is a plain Unity Package Manager package stored in a subdirectory:

- `UnityPackage/` -> `com.osuframework.unity`

That means consumers install it directly from a Git URL with a `?path=` suffix, or from a local path while developing. There is no npm/OpenUPM/scoped-registry publishing step yet.

### Package Names

| Package | Path | Purpose |
| :--- | :--- | :--- |
| `com.osuframework.unity` | `UnityPackage` | Core hierarchy-aware osu!framework DI adapter for Unity. |

### Git Install

Use the repository URL with a package path. Replace the URL with the actual remote for your fork or upstream repo.

```json
{
  "dependencies": {
    "com.cysharp.r3": "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
    "com.osuframework.unity": "https://github.com/YOUR_ORG/osu-framework-unity-di.git?path=/UnityPackage"
  }
}
```

Pinning to a tag or commit is recommended once releases exist:

```text
https://github.com/YOUR_ORG/osu-framework-unity-di.git?path=/UnityPackage#v0.1.0-preview
```

### Local Development Install

For local validation or package development, use file dependencies:

```json
{
  "dependencies": {
    "com.cysharp.r3": "file:B:/_validation_deps/R3/src/R3.Unity/Assets/R3.Unity",
    "com.osuframework.unity": "file:B:/osu-framework-unity-di/UnityPackage"
  }
}
```

The validation project in this workspace uses that model.

### R3 Prerequisite

This package requires [R3](https://github.com/Cysharp/R3). The simplest UPM path is:

```text
https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity
```

If your project uses NuGetForUnity for the core `R3.dll`, keep `R3.Unity` installed through UPM and ensure the required NuGet assemblies are available to Unity. Set Unity's API Compatibility Level to `.NET Standard 2.1` if your project is not already configured that way.

### Import Samples

After installing the package, open Unity Package Manager, select the package, and import the listed sample:

- `com.osuframework.unity`: `Basic Dynamic UI`


## Validating Changes

There is no CI job yet, so local validation is currently the source of truth. This workspace has been validated with Unity `6000.4.6f1` using:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath B:\OsuDiValidation `
  -runTests `
  -testPlatform PlayMode `
  -testResults B:\OsuDiValidation\Logs\screens-test-results.xml `
  -logFile B:\OsuDiValidation\Logs\screens-tests.log
```

The validation project should mark both packages testable when running package tests:

```json
{
  "testables": [
    "com.osuframework.unity"
  ]
}
```

## Future CI/CD Shape

If this becomes a published package, the likely CI/CD flow is:

1. Run Unity PlayMode package tests against a validation project.
2. Pack or release by tagging the repository.
3. Consume packages through Git tags using `?path=/UnityPackage#tag`.
4. Optionally publish to OpenUPM or a private scoped registry.

Until that exists, do not assume package publication happens automatically. Treat Git path installs as the supported distribution mechanism.

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
