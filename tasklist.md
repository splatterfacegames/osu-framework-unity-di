# Hyper-Granular Task List: osu!framework DI for Unity

## Phase 1: Repository Setup & Forking

### 1.1 Repository Preparation
- [ ] Create a new GitHub repository for the Unity package wrapper (e.g., `osu-framework-unity-di`).
- [ ] Add `ppy/osu-framework` as a Git submodule.
- [ ] Create a `UnityPackage` folder at the root of the new repository.
- [ ] Initialize standard Unity package structure inside `UnityPackage/` (`package.json`, `Runtime/`, `Editor/`, `Tests/`).
- [ ] Configure `.gitignore` for Unity and .NET source generator projects.

### 1.2 Upstream Dependency Analysis
- [ ] Search the `osu.Framework.Allocation` namespace for any direct references to `osu.Framework.Graphics`.
- [ ] Search for `IReadOnlyDependencyContainer` and `DependencyContainer` usages that expect visual elements (e.g., `Drawable`).
- [ ] Map out all static caches or global states in `DependencyActivator` that might conflict with Unity's domain reload settings.

### 1.3 Decoupling & Interface Abstraction
- [ ] Introduce or refine `IDependencyInjectionCandidate` to ensure it does not imply a `Drawable` hierarchy.
- [ ] Refactor any `CompositeDrawable` specific dependency merging logic into a generic tree-walker interface (if present in core Allocation).
- [ ] Ensure `DependencyActivator.Activate()` can accept any object without assuming it inherits from a specific UI class.
- [ ] Verify the modified `osu.Framework` code compiles standalone (`dotnet build` on the Allocation project).

---

## Phase 2: Unity Adapter Implementation (Reflection-Based MVP)

### 2.1 Core Unity Wrappers
- [ ] Create `OsuFramework.Unity.Allocation.asmdef` inside `UnityPackage/Runtime/`.
- [ ] Link the `osu.Framework` (specifically Allocation components) to the Unity project (via source inclusion, symlinks, or compiled DLL).
- [ ] Create `IDependencyNode` interface containing an `IReadOnlyDependencyContainer`.

### 2.2 Hierarchy Walking Logic
- [ ] Implement `DependencyNodeBehaviour : MonoBehaviour, IDependencyNode`.
- [ ] Implement `CreateChildDependencies()` logic within `DependencyNodeBehaviour` to merge `[Cached]` items with the parent container.
- [ ] Write a static helper `DependencyExtensions.GetParentNode(Transform t)` that loops `t.parent` to find the nearest `IDependencyNode`.

### 2.3 `MonoBehaviour` Injection Hook
- [ ] Implement base class `DependencyBehaviour : MonoBehaviour, IDependencyInjectionCandidate`.
- [ ] Implement `Awake()` inside `DependencyBehaviour`:
  - [ ] Look up the parent `IDependencyNode` via `DependencyExtensions.GetParentNode(transform.parent)`.
  - [ ] If no parent is found, default to an empty or global `DependencyContainer`.
  - [ ] Call `DependencyActivator.Activate(this, parentContainer)`.
- [ ] Ensure `DependencyActivator.Activate()` successfully invokes `[BackgroundDependencyLoader]` methods via Reflection.
- [ ] Implement `OnDestroy()` inside `DependencyBehaviour` to handle any necessary cache cleanup (if a node itself was providing dependencies).

### 2.4 Reflection MVP Testing
- [ ] Create a Unity Test Runner scene.
- [ ] Create `ParentBehaviour` (with `[Cached] string MyString = "Hello";`).
- [ ] Create `ChildBehaviour` (with `[Resolved] string InjectedString;`).
- [ ] Instantiate parent, instantiate child under parent, verify `InjectedString` is "Hello".
- [ ] Verify dynamic instantiation: Instantiate prefab child at runtime under parent, confirm immediate injection.

### 2.5 R3 Integration (Reactive State)
- [ ] Update `UnityPackage/package.json` to include R3 as a dependency.
- [ ] Modify `DependencyActivator` and `SourceGeneratorUtils` to identify `ReactiveProperty<T>` and `ReadOnlyReactiveProperty<T>`.
- [ ] Implement seamless injection for `[Resolved]` R3 properties.
- [ ] Ensure any automatic 'rebinding' logic uses R3 subscriptions.
- [ ] Update `DependencyBehaviour` to hold a `CompositeDisposable` that is disposed of in `OnDestroy()`, and tie reactive property subscriptions to this lifecycle.

---

## Phase 3: Source Generator Integration

### 3.1 Source Generator Compilation
- [ ] Analyze `ppy.osu.Framework.SourceGeneration` `.csproj`.
- [ ] Modify the build process to output a `.dll` compatible with Unity's Roslyn analyzer requirements (.NET Standard 2.0).
- [ ] Write a script or CI step to automatically build and copy `ppy.osu.Framework.SourceGeneration.dll` into `UnityPackage/Editor/Analyzers/`.

### 3.2 Unity Analyzer Configuration
- [ ] Select the `.dll` in Unity and set its Asset Labels to `RoslynAnalyzer`.
- [ ] Uncheck "Any Platform" and "Include Platforms" (ensure it acts purely as a compiler analyzer, not a runtime DLL).
- [ ] Trigger a Unity script recompile.

### 3.3 Validating Source Generation
- [ ] Verify the source generator correctly identifies `DependencyBehaviour` derived classes.
- [ ] Check Unity's `obj/` compilation folder to find the generated `ISourceGeneratedDependencyActivator` proxy files.
- [ ] Resolve any namespace or partial class conflicts caused by Unity's default `.csproj` structure.
- [ ] Verify `DependencyActivator.Activate()` automatically uses the generated proxy instead of falling back to Reflection.

### 3.4 Performance Profiling
- [ ] Create a stress-test scene with 10,000 prefabs instantiating simultaneously.
- [ ] Profile the instantiation using Unity Profiler.
- [ ] Confirm `System.Reflection` is **not** present in the deep profile stack trace during instantiation.
- [ ] Confirm zero-allocation (0 bytes GC alloc) during dependency resolution.

---

## Phase 4: Refinement & MVP Release

### 4.1 Optimization
- [ ] Optimize the `Transform.parent` lookup (e.g., caching the parent reference if the transform hierarchy hasn't changed).
- [ ] Handle Unity's Domain Reload: ensure `DependencyActivator.ClearCache()` (or equivalent) is called on `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` to clear static maps.

### 4.2 Documentation
- [ ] Write `README.md` containing:
  - [ ] Value proposition (Why choose this over VContainer / Extenject).
  - [ ] Installation instructions (via Git URL in Unity Package Manager).
  - [ ] Quick start guide (how to use `[Cached]`, `[Resolved]`, and `[BackgroundDependencyLoader]`).
- [ ] Create a `Sample~` folder inside the package with a fully documented example scene showing dynamic UI instantiation.

### 4.3 Release
- [ ] Update `package.json` with version `0.1.0-preview`.
- [ ] Tag the release on GitHub.
- [ ] Provide a clean Git URL for users to add via the Unity Package Manager.
- [ ] Test importing the package into a completely empty, fresh Unity project to ensure no hidden dependencies.