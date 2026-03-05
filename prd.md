# Product Requirements Document (PRD): osu!framework DI for Unity

## 1. Executive Summary & Goals
### 1.1 What We Are Building
We are creating a new Unity package that ports the highly performant, source-generated Dependency Injection (DI) system from ppy's `osu!framework` (`osu.Framework.Allocation`) into the Unity ecosystem. 

### 1.2 The Problem
Unity developers are currently forced to choose between:
- **Developer Experience (Extenject/Zenject):** Excellent workflow using attributes and hierarchy-aware scoping, but crippled by runtime reflection costs and heavy initializers that cause frame drops.
- **Performance (VContainer):** Excellent performance using Source Generators (zero-allocation), but enforces rigid constructor injection which fights the `MonoBehaviour` lifecycle and requires cumbersome boilerplate for dynamic prefab instantiation.

### 1.3 The Solution & Value Proposition
The `osu!framework` DI system solves both. It relies on attribute-based field/property injection (`[Resolved]`, `[Cached]`) and method injection (`[BackgroundDependencyLoader]`), completely avoiding the need for constructor injection. It maps naturally to visual hierarchies and uses C# Source Generators to resolve all dependencies at compile-time. 

This project will give Unity developers the "Holy Grail" of DI: the exact workflow and hierarchy-awareness of Extenject, with the zero-allocation compile-time performance of VContainer.

---

## 2. Architecture & The Fork Strategy
### 2.1 The Core Strategic Constraint
To maintain long-term viability, this project will be maintained as a **fork** of the upstream `ppy/osu-framework` repository. We want to avoid rewriting the DI logic so that we can easily `git rebase` or pull bug fixes from ppy.

### 2.2 Decoupling `Allocation` from `Drawable`
Currently, `osu.Framework.Allocation` assumes dependencies are flowing through `osu.Framework.Graphics.Drawable`. To decouple this:
1. **Interface Abstraction:** Ensure the core `DependencyContainer` and `DependencyActivator` rely solely on interfaces (like `IDependencyInjectionCandidate` and `IReadOnlyDependencyContainer`) rather than concrete `Drawable` types.
2. **Minimal Upstream Changes:** We will make surgical, upstream-compatible changes (potentially even submitting them back to ppy if they improve generic decoupling) to ensure the `Allocation` namespace does not strictly require `osu.Framework.Graphics`.
3. **Unity Adapter Layer:** We will introduce a Unity-specific wrapper assembly (e.g., `OsuFramework.Unity.Allocation`) that provides the concrete implementations mapping `IDependencyInjectionCandidate` to `MonoBehaviour` and `Transform`.

### 2.3 Repository Structure
- `osu-framework/` (Forked upstream submodule or branch)
  - `osu.Framework/Allocation/` (Kept as close to upstream as possible)
  - `osu.Framework.SourceGeneration/` (Unmodified upstream analyzer)
- `UnityPackage/` (Our wrapper)
  - `Runtime/` (Contains Unity `MonoBehaviour` adapters and `Transform` hierarchy walkers)
  - `Editor/` (Contains Unity Editor tools and the compiled Source Generator DLLs)

---

## 3. Core Features & Mapping
### 3.1 Mapping `[Cached]` to `Transform`
In Unity, the `Transform` hierarchy will act as the scoping mechanism. 
- A `MonoBehaviour` can use `[Cached]` on its class or fields. 
- We will provide a `DependencyNode` component (or similar base class/interface) that attaches to a GameObject. This node holds an `IReadOnlyDependencyContainer`.
- When a child object requests a dependency, the system will walk up the `Transform.parent` hierarchy until it finds a `DependencyNode` that can resolve the request.

### 3.2 Mapping `[Resolved]` and `[BackgroundDependencyLoader]`
- `[Resolved]` will function identically to how it does in osu!framework, injecting dependencies into fields or properties.
- `[BackgroundDependencyLoader]` will act as the DI-safe initialization method. It maps conceptually to Unity's `Awake` or `Start`. 

### 3.3 Unity Lifecycle Integration
Since we cannot control when Unity instantiates a `MonoBehaviour`, we must hook into its lifecycle:
- **`Awake()`:** The adapter will trigger the dependency resolution process. It will look up the `Transform.parent` chain, build the local `DependencyContainer`, inject `[Resolved]` fields, and invoke the `[BackgroundDependencyLoader]` method.
- **`OnDestroy()`:** The adapter will clean up local caches to prevent memory leaks, mirroring how `Drawable` disposal works.

---

## 4. Technical Integration (Unity specifics)
### 4.1 Source Generator Integration
Unity supports Roslyn Analyzers and Source Generators out-of-the-box (Unity 2021.3+).
- We will compile `ppy.osu.Framework.SourceGeneration` into a standard `.dll`.
- In the Unity package, this `.dll` will be imported and marked with the `RoslynAnalyzer` asset label. Unity's compiler will automatically run it, generating the proxy injection code in the `obj/` folder behind the scenes.

### 4.2 Handling Dynamic Instantiation
When calling `Instantiate(prefab, parent)`, Unity triggers `Awake` immediately on the clone. 
- **The Flow:** Because we set the `parent` during instantiation, our `Awake` hook inside the cloned `MonoBehaviour` will naturally look at `transform.parent`, find the parent's `DependencyContainer`, and resolve all dependencies instantly using the source-generated pathways.
- **Result:** Frictionless, zero-allocation dynamic prefab instantiation without needing a heavy `DiContainer.InstantiatePrefab()` wrapper method.

---

## 5. Out of Scope
To ensure focus and maintainability, the following `osu!framework` systems are explicitly **NOT** being ported:
1. **The `Drawable` UI and Rendering Pipeline:** Unity has its own rendering pipelines and UI systems.
2. **`Bindable<T>` (Reactive State):** Unity has UniRx and native UI Toolkit bindings.
3. **Input and Audio Systems:** Unity has the New Input System and its own Audio pipeline.
4. **`VisualTests` / TestBrowser:** While valuable, this is outside the scope of a pure DI package and would require massive rendering porting.

---

## 6. Milestones & Implementation Phases

### Phase 1: Repository Setup & Forking
- Fork `ppy/osu-framework`.
- Analyze tight couplings between `osu.Framework.Allocation` and `osu.Framework.Graphics`.
- Implement interface-level abstractions in the fork to decouple Allocation.

### Phase 2: Unity Adapter Implementation (Reflection-based)
- Setup the Unity Package structure (`Runtime`, `Editor`).
- Implement the `DependencyNode` logic that walks `Transform.parent`.
- Hook `DependencyActivator.Activate()` into a base `DependencyBehaviour` (which inherits from `MonoBehaviour`)'s `Awake` method.
- *Goal:* Get the DI working in Unity using the fallback Reflection pathway (to prove the hierarchy logic works).

### Phase 3: Source Generator Integration
- Compile the `osu.Framework.SourceGeneration` project into a Roslyn Analyzer `.dll`.
- Import the `.dll` into Unity and configure the asset labels.
- Verify that Unity generates the `ISourceGeneratedDependencyActivator` partial classes for our Unity `MonoBehaviour`s.
- *Goal:* Verify zero-allocation dependency resolution in the Unity Profiler.

### Phase 4: Refinement & MVP Release
- Optimize `Transform.parent` lookups (e.g., caching the nearest `DependencyNode`).
- Write comprehensive Unity-specific documentation.
- Publish MVP package to a Git URL for testing in real Unity projects.