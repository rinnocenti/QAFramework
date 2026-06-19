# Immersive Pooling

Specialized package for generic pooling primitives.

## v0 Freeze

- `Pooling v0` is frozen with `IPoolable`, `PoolableBehaviour`, `GameObjectPool`, and `PoolReturnHandle`.
- `Immersive.Pooling.Runtime` remains pure and does not reference `UnityEngine`.
- `Immersive.Pooling.Unity` contains the Unity adapters.
- `GameObjectPool` is the local, instantiable Unity pool core.
- `PoolReturnHandle` is the local reference from an instance back to its origin pool.

## Boundary

- Pooling is a separate package boundary and is not part of `com.immersive.foundation`.
- This package is not framework lifecycle.
- This package is not a global bootstrap.
- This package is not a direct replacement for the old `PoolService`.
- `Immersive.Pooling.Runtime` must remain pure and free of `UnityEngine`.
- `Immersive.Pooling.Unity` is the separate adapter layer for Unity-specific pooling concerns.
- `IPoolable` is the active pure runtime contract.
- `IPoolableObject` from the old pooling block is replaced by the cleaner public name `IPoolable`.
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
- Future responsibilities allowed in this package are generic pooling contracts, technical handles, explicit return semantics, Unity `GameObject` pooling, technical instance hosting, and generic capacity or prewarm policies when they do not depend on framework lifecycle.
- Activity/Route lifecycle, Actor/Projectile/Audio pooling, old `PoolDefinitionAsset`, old `PoolService`, QA helpers, service locator, singleton requirement, fallback silence, and mandatory authoring workflows are out of scope.

## v0 Skeleton

- Runtime pure contracts are added incrementally.
- Unity adapters start with `PoolableBehaviour`.
- `GameObjectPool` owns local activation/deactivation and instance reuse.
- `PoolReturnHandle` owns explicit local return-to-origin behavior.
- Canonical order is `Take`: bind handle -> set active -> notify `IPoolable`; `Return`: notify `IPoolable` -> set inactive -> mark available; `Clear`: clear binding -> destroy.
- Route/Activity-scoped pools, Projectile/Audio pools, and Inspector-driven config stay for later cuts outside this core minimum.
- No runtime migration is included in this cut.
