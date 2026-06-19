# Pooling Boundary

`com.immersive.pooling` is a specialized package for generic pooling primitives.

## Pooling v0 Freeze

- `IPoolable` is the active pure runtime contract.
- `PoolableBehaviour` is the minimum Unity adapter.
- `GameObjectPool` is the local, instantiable Unity pool core.
- `PoolReturnHandle` is the local instance-to-origin-pool reference.
- `Immersive.Pooling.Runtime` stays pure and free of `UnityEngine`.
- `Immersive.Pooling.Unity` holds Unity adapters only.

## Boundary Rules

- Pooling is not framework lifecycle.
- Pooling is not a global bootstrap.
- Pooling is not a direct replacement for the old `PoolService`.
- Runtime code in `Immersive.Pooling.Runtime` must stay pure and must not reference `UnityEngine`.
- Unity-specific pooling concerns belong in `Immersive.Pooling.Unity`.
- `IPoolable` is the active pure runtime contract.
- `IPoolableObject` from the old block is replaced by the cleaner public name `IPoolable`.
- `PoolableBehaviour` is the minimum Unity adapter for `MonoBehaviour`-based poolables.
- `PoolableBehaviour` replaces the old `PooledBehaviour` name for public clarity.
- `PoolableBehaviour` is not a pool, host, service, lifecycle owner, or reset system.
- `GameObjectPool` is the local, instantiable Unity pool core for `GameObject`.
- `GameObjectPool` is not a service global, host, lifecycle owner, or authoring asset.
- `GameObjectPool` is the technical owner of the instances it creates.
- `PoolReturnHandle` is the local Unity component for explicit return to the origin pool.
- `PoolReturnHandle` replaces the intent of the old `PoolAutoReturnTracker` without copying its shape.
- `PoolReturnHandle` does not transfer ownership; it only stores a local reference to the origin pool.
- `PoolReturnHandle` is not a service, host, lifecycle owner, or auto-release policy.
- Auto-return by time, event, collision, or lifecycle is deferred to future integrations.
- The package may later contain generic poolable contracts, technical handles, return semantics, Unity `GameObject` pooling, technical instance hosts, and generic capacity or prewarm policies if they stay independent from framework lifecycle.
- The package does not own `Activity`, `Route`, `Actor`, `Projectile`, `Audio`, session lifecycle, reset/release orchestration, or framework bootstrap behavior.
- The package does not own `PoolDefinitionAsset`, `PoolLifetimeScope`, or `PoolRegistrationMode` from the old pooling block.
- The package does not require service locator behavior, singleton ownership, hidden global config, or fallback silence.
- Authoring and Inspector workflows are deferred until the runtime boundary is stable.

## Explicit Exclusions

- Old `PoolService`
- Old `PoolDefinitionAsset`
- Old `IPoolableObject`
- Unity adapter `PooledBehaviour`
- `GlobalCompositionRoot`
- Old `PoolAutoReturnTracker`
- host global
- singleton/global
- service locator
- bootstrap automatico
- hidden global config
- mandatory authoring/Inspector
- Actor, Projectile, and Audio pooling
- Session, Route, and Activity lifecycle
- Activity reset/release
- Global/Route/Activity scope policies
- QA helpers
- silent fallback

## Canonical Order

- `Take`: bind handle -> set active -> notify `IPoolable`
- `Return`: notify `IPoolable` -> set inactive -> mark available
- `Clear`: clear binding -> destroy
- Rejected returns are expected and return `false`.

## v0 Cut

- `Pooling v0` is frozen.
- `PoolableBehaviour` is the first Unity adapter cut.
- `GameObjectPool` is the first local Unity pool core cut.
- `PoolReturnHandle` is the first explicit return component cut.
- Route/Activity-scoped pools, Projectile/Audio pools, and Inspector-driven config are deferred to later cuts outside this core minimum.
- No runtime migration is included in this cut.
