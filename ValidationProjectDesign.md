# Unity Validation Project Design

This project is meant to answer one question:

Can a normal Unity project install this package and use it successfully for real `MonoBehaviour` dependency injection?

## Goal

Validate the package in the smallest project that still covers the risky parts:

- Git URL package import
- analyzer/source generator execution
- `[Cached]` and `[Resolved]` injection
- `[BackgroundDependencyLoader]` invocation
- hierarchy override behavior
- dynamic prefab instantiation
- R3 reactive rebinding
- editor recompilation cache invalidation

This is not a showcase project. It is a proof project.

## Unity Version

Use:

- Unity `2021.3 LTS` or newer
- API Compatibility Level `.NET Standard 2.1`

## External Packages

Install in this order:

1. `R3` via NuGetForUnity
2. `R3.Unity` via git URL
3. this package via git URL:
   `https://github.com/jethac/osu-framework-unity-di.git?path=/UnityPackage`

## Project Shape

Create a new Unity project called `OsuDiValidation`.

Use one scene:

- `Assets/Scenes/ValidationScene.unity`

Use one folder for scripts:

- `Assets/Scripts/Validation/`

Use one prefab folder:

- `Assets/Prefabs/`

## Scene Design

The scene should contain:

1. `ValidationRoot`
2. `OverrideZone`
3. `UI`

`ValidationRoot` is the main provider.

`OverrideZone` is a child object that overrides one dependency and spawns a local prefab instance.

`UI` is a Canvas with simple text buttons to trigger runtime checks.

## Runtime Objects

Create these objects:

### `ValidationRoot`

Attach `ValidationRootBehaviour`.

Responsibilities:

- cache a `string` environment name
- cache an `int` base score
- cache `ReactiveProperty<int>` shared score
- increment score every second
- expose buttons for spawning prefabs
- log success/failure markers

### `OverrideZone`

Child of `ValidationRoot`.

Attach `OverrideNodeBehaviour`.

Responsibilities:

- override the cached `string` environment name
- optionally override another scalar value
- prove nearest-parent resolution works

### `DisplayPrefab`

Prefab with `DisplayConsumerBehaviour`.

Responsibilities:

- receive resolved scalar values
- receive resolved `ReactiveProperty<int>`
- subscribe in `[BackgroundDependencyLoader]`
- render/log current values

### `LoaderProbe`

Optional child object with `LoaderProbeBehaviour`.

Responsibilities:

- inject via method parameters only
- verify `[BackgroundDependencyLoader]` parameter resolution works without `[Resolved]` properties

## Scripts

Implement these scripts under `Assets/Scripts/Validation/`.

### `ValidationRootBehaviour`

Inherit from `DependencyNodeBehaviour`.

Cache:

- `[Cached] private string environmentName = "Root";`
- `[Cached] private int baseScore = 100;`
- `[Cached] public ReactiveProperty<int> SharedScore { get; } = new(0);`

Behavior:

- increment `SharedScore` once per second
- instantiate one `DisplayPrefab` under `ValidationRoot`
- instantiate one `DisplayPrefab` under `OverrideZone`
- provide a public method to spawn more displays at runtime

Pass criteria:

- root child resolves `environmentName = "Root"`
- override child resolves `environmentName = "Override"`
- both see the same live `SharedScore` stream unless intentionally overridden

### `OverrideNodeBehaviour`

Inherit from `DependencyNodeBehaviour`.

Cache:

- `[Cached] private string environmentName = "Override";`

Pass criteria:

- children under this object resolve the override instead of the root value

### `DisplayConsumerBehaviour`

Inherit from `DependencyBehaviour`.

Resolve:

- `[Resolved] private string environmentName { get; set; }`
- `[Resolved] private int baseScore { get; set; }`
- `[Resolved] private ReactiveProperty<int> sharedScore { get; set; }`

Loader:

- `[BackgroundDependencyLoader] private void load()`

Behavior:

- log resolved values once on load
- subscribe to `sharedScore`
- update a TMP text label if present

Pass criteria:

- object compiles as `partial`
- source generation works with no manual registration
- load method fires automatically
- spawned instances immediately receive valid dependencies

### `LoaderProbeBehaviour`

Inherit from `DependencyBehaviour`.

No `[Resolved]` properties.

Use:

- `[BackgroundDependencyLoader] private void load(string environmentName, int baseScore)`

Pass criteria:

- method receives injected parameters correctly
- proves method-parameter injection works independently of property injection

### `MissingDependencyProbeBehaviour`

Keep this on a disabled test object or spawn it deliberately from a button.

Resolve:

- a type that is not cached anywhere

Pass criteria:

- it throws a predictable `DependencyNotRegisteredException`
- the failure is easy to spot in the Console

## UI

Keep the UI minimal:

- `Spawn Root Display`
- `Spawn Override Display`
- `Spawn Missing Dependency Probe`
- `Hot Reload Check`

Use TextMeshPro if convenient, but plain logs are enough if you want zero extra setup.

## Validation Steps

Run these steps in order.

### 1. Package Import

Expected result:

- package appears in Package Manager
- no missing assembly errors
- analyzer DLL imports without manual steps

Fail if:

- package import requires editing package files by hand
- compile errors appear before writing any project code

### 2. Source Generator Check

Create all validation behaviours as `partial`.

Expected result:

- project compiles cleanly
- injected members work at runtime

Fail if:

- partial classes compile but injection never happens
- users must manually implement generated interfaces

### 3. Hierarchy Injection Check

Start the scene.

Expected result:

- root-spawned consumer logs `environmentName=Root`
- override-spawned consumer logs `environmentName=Override`

Fail if:

- child objects do not see nearest cached value

### 4. Dynamic Instantiation Check

Press `Spawn Root Display` and `Spawn Override Display`.

Expected result:

- newly spawned prefabs inject immediately on `Awake`
- subscriptions start without extra bootstrap code

Fail if:

- spawned prefabs need manual injection calls

### 5. Reactive Rebinding Check

Let score tick for several seconds.

Expected result:

- all displays update continuously
- newly spawned display catches up immediately to current value

Fail if:

- resolved reactive properties stop updating
- destroyed objects leak subscriptions

### 6. Missing Dependency Check

Press `Spawn Missing Dependency Probe`.

Expected result:

- deterministic failure in Console
- clear exception type and message

Fail if:

- silent failure
- null injection where a required dependency should fail loudly

### 7. Editor Recompile / Hot Reload Check

While the editor is open:

1. enter Play Mode
2. change a dependency signature in `DisplayConsumerBehaviour`
3. let Unity recompile
4. spawn a fresh display

Suggested edits:

- rename one resolved property
- add a new `[Resolved] private int baseScore { get; set; }`
- change the `[BackgroundDependencyLoader]` parameter list

Expected result:

- newly spawned objects use the new metadata after compile/reload
- no stale activator behavior

Fail if:

- new instances still behave as if old members exist
- only a full editor restart fixes DI

## Success Definition

Call the package "usable in Unity" if all of these are true:

- package installs into a blank project from Git URL
- no package file edits are required by the consumer
- simple scene setup works with ordinary `MonoBehaviour`s
- runtime-instantiated prefabs inject correctly
- R3 bindings behave correctly
- failure modes are understandable
- editor recompilation does not leave stale DI activators behind

## Nice-To-Have Extras

If the base validation passes, add these follow-ups:

- prefab variant under nested override scopes
- additive scene load test
- Enter Play Mode Options with domain reload disabled
- IL2CPP build smoke test
- sample scene packaged under `Samples~`

## Recommendation

If you want the fastest path, do not create a full game. Build exactly this:

- one scene
- three to five scripts
- one prefab
- four UI buttons

That is enough to validate the package honestly.
