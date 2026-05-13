# Integration Examples

This package should be validated with examples that look like normal Unity project architecture, not only minimal attribute checks.

The goal of a true integration test is to answer:

Can a Unity project build a normal gameplay or UI slice from scenes, prefabs, services, and reactive state using this package as the composition mechanism?

## What DI Is Usually Used For

In Unity, dependency injection is usually a replacement for:

- global singletons such as `GameManager.Instance`
- manual inspector wiring across many prefabs
- repeated `GetComponentInParent<T>()`
- service locators
- hard-coded prefab setup scripts
- static state that is hard to reset in tests

Common injected dependencies include:

- game or session state, such as score, lives, selected level, current player
- services, such as audio, save data, analytics, input, localization, asset loading
- model/state streams, such as reactive score, health, settings, inventory
- factories/spawners, such as projectile factories, popup factories, enemy factories
- configuration, such as difficulty, theme, feature flags
- UI view-model data, such as HUD score, menu state, player profile
- scoped context overrides, such as different room, zone, team, or player context

In this package, DI manifests as hierarchy scope:

- a parent `DependencyNodeBehaviour` provides `[Cached]` values
- child prefabs use `[Resolved]` properties or `[BackgroundDependencyLoader]` parameters
- nested nodes override dependencies for their subtree
- runtime-instantiated prefabs receive dependencies automatically during `Awake`
- R3 state can be injected and disposed with the receiving behaviour

## Comparison-Inspired Workflow

The Unity workflow should make two dependency shapes obvious:

- ambient services that belong to a hierarchy scope
- explicit arguments that are required for one object instance

Use hierarchy DI for values that should be inherited by a subtree:

```csharp
public partial class PlayerZone : DependencyNodeBehaviour
{
    [Cached]
    public ReactiveProperty<int> Health { get; } = new ReactiveProperty<int>(100);

    [Cached]
    public PlayerContext Player { get; } = new PlayerContext(0);
}

public partial class PlayerHud : DependencyBehaviour
{
    [Resolved]
    private ReactiveProperty<int> health { get; set; }
}
```

Use explicit initialization for values that are part of one instance's construction, such as a screen id, save slot, initial tab, route payload, selected item, or player index. Implement `IInitializable<T>` and pass those values through the object creation API:

```csharp
await navigator.PushAsync(
    inventoryScreenPrefab,
    new InventoryRouteArgs(playerIndex: 0, initialTab: "equipment"));
```

This callback is deliberately separate from `[Resolved]`: `[Resolved]` describes what the hierarchy provides, while `Init(...)` describes what the caller must choose for this exact instance.

For values that must be visible to `[BackgroundDependencyLoader]`, use `InstantiateWithArguments` or the screen stack's argument overloads. They create the clone inactive, call `Init(...)`, and then restore the instance's active state so the loader can read the initialized value during `Awake`.

Inspector support should reinforce the same distinction:

- show `[Cached]` values provided by the selected object
- show `[Resolved]` values required by the selected object
- show whether each dependency resolves from self, parent, or an ancestor
- warn when a required dependency is missing in the current hierarchy
- allow a prefab preview context to supply mock providers when the prefab is inspected outside its final scene parent

That gives osu!framework DI the same object-initialization clarity people expect from `Init(args)`, while keeping hierarchy scope as the main composition model.

## Recommended Integration Project

Build one small validation project with one scene that behaves like a small gameplay loop.

### Scene Shape

```text
GameRoot
├── GameplayArea
│   ├── PickupPrefab instances
│   └── PopupPrefab instances
└── PlayerZone
    ├── HUD
    └── Player-specific PopupPrefab instances
```

### GameRoot

`GameRoot` should inherit from `DependencyNodeBehaviour`.

It should cache app-level services and state:

- `IAudioService`
- `IAnalyticsService`
- `ReactiveProperty<int> Score`
- `GameSession`
- `LevelConfig`

This proves root-level dependencies can be shared by all children.

### GameplayArea

`GameplayArea` should inherit from `DependencyNodeBehaviour`.

It should cache gameplay-scoped objects:

- `IEnemySpawner`
- `RoundState`
- optionally an overridden `LevelConfig`

This proves a scene subtree can provide local gameplay dependencies.

### PlayerZone

`PlayerZone` should inherit from `DependencyNodeBehaviour`.

It should cache player-specific context:

- `PlayerContext`
- `InputContext`
- `ReactiveProperty<int> Health`

This proves nearest-parent dependency resolution and scoped overrides.

### HUD Prefab

`HUD` should inherit from `DependencyBehaviour`.

It should resolve:

- `ReactiveProperty<int> Score`
- `ReactiveProperty<int> Health`
- `PlayerContext`

It should subscribe in `[BackgroundDependencyLoader]`, update visible UI or observable test state, and dispose subscriptions when destroyed.

### Pickup Prefab

`PickupPrefab` should inherit from `DependencyBehaviour`.

It should resolve:

- `GameSession`
- `ReactiveProperty<int> Score`
- `IAudioService`
- `IAnalyticsService`

When collected, it should increment score, play a sound, and report an analytics event. This proves runtime-instantiated prefabs can consume services immediately without a custom factory/injection call.

### Popup Prefab

`PopupPrefab` should inherit from `DependencyBehaviour`.

Instantiate it under different parents:

- under `GameRoot`, where it receives the global theme/context
- under `PlayerZone`, where it receives the player-specific theme/context

This proves hierarchy overrides affect ordinary prefab workflows.

## Integration Test Cases

A useful PlayMode test suite should verify user-facing behaviour, not only container mechanics.

1. Package import and compile
   A fresh Unity project installs the package via Git URL or local package path, installs R3 exactly as documented, and compiles cleanly.

2. Source generator active
   Representative behaviours implement `ISourceGeneratedDependencyActivator`, proving the Roslyn analyzer/source generator executed.

3. Root service injection
   `HUD` resolves `GameSession`, `Score`, and services from `GameRoot`.

4. Scoped override
   Two HUD instances under different `PlayerZone`s resolve different `PlayerContext`s while sharing the same root `Score`.

5. Dynamic prefab instantiation
   Runtime-instantiated pickup and popup prefabs receive dependencies immediately during `Awake` / `[BackgroundDependencyLoader]`.

6. Explicit screen initialization
   Push an inactive screen prefab with a route argument callback, assert the screen's `Init(...)` method ran before `[BackgroundDependencyLoader]`, and assert the screen still resolved ambient hierarchy services from its stack.

7. Reactive state propagation
   Changing `Score.Value` updates every HUD. Changing one player's `Health.Value` updates only that player's HUD.

8. Lifecycle disposal
   Destroy a HUD, mutate score/health again, and assert no callback fires and no disposed-object errors are logged.

9. Missing dependency behaviour
   Spawn a deliberately bad prefab and assert Unity logs the expected `DependencyNotRegisteredException`.

10. Scene reload / domain reload cache behaviour
   Reload the scene or re-enter play mode and assert dependency activation still works. This validates static activator cache clearing.

11. Inspector validation and preview context
    In edit mode, select representative scene objects and prefabs, then assert the editor reports cached providers, resolved consumers, missing dependencies, and preview-context providers consistently.

12. Build validation
    Build a standalone player, not only editor PlayMode tests. This catches analyzer/plugin importer mistakes and runtime dependency packaging issues.

## Success Criteria

The package is doing what it claims if a Unity developer can:

- put a provider above a consumer in the transform hierarchy
- instantiate prefabs normally with `Object.Instantiate`
- avoid special injection calls
- avoid singleton/service-locator access from consumers
- get source-generated activation in user behaviours
- use scoped overrides naturally through nested `DependencyNodeBehaviour`s
- pass explicit per-instance arguments without turning them into global or static state
- inspect provider/consumer relationships in the Unity editor before entering Play Mode
- rely on reactive subscriptions being cleaned up with Unity object lifetime
