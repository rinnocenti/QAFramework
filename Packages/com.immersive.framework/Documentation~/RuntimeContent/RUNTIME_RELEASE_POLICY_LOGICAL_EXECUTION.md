# Runtime Release Policy / Logical Execution

Status: `F8J APPLIED / PENDING COMPILE-SMOKE`

F8J adds the logical release side of RuntimeContent. It does not add physical cleanup.

The core now has:

```text
RuntimeReleaseRequest
RuntimeReleaseResult
RuntimeReleaseStatus
RuntimeReleasePolicy
IRuntimeReleaseAdapter
RuntimeContentRuntime.ReleaseHandleLogically(...)
RuntimeContentRuntime.ReleaseScopeLogically(...)
RuntimeContentRuntime.ApplyReleaseResult(...)
```

## Boundary

RuntimeContent core owns:

```text
owner identity
scope context
handle state
logical release policy
release result diagnostics
registry unregister
```

RuntimeContent core does not own:

```text
Destroy
UnloadSceneAsync
Addressables.Release
pool return
GameObject hierarchy cleanup
Content Anchor binding release
Actor/Pause/Camera/UI cleanup
```

Those operations belong to explicit physical adapters or later consumers.

## Canonical release chain

```text
RuntimeScopeContext
  -> RuntimeReleaseRequest
      -> optional IRuntimeReleaseAdapter.Release
          -> RuntimeReleaseResult
              -> RuntimeContentRuntime.ApplyReleaseResult
                  -> RuntimeContentHandle.RequestRelease
                  -> RuntimeContentHandle.MarkReleased
                  -> optional unregister from RuntimeScopeRoot
```

For pure logical handles, the runtime can call:

```text
RuntimeContentRuntime.ReleaseHandleLogically(...)
RuntimeContentRuntime.ReleaseScopeLogically(...)
```

This changes handle/registry state only.

## Policies

| Policy | Meaning |
|---|---|
| `MarkReleasedOnly` | Mark the handle as `Released`, but keep it registered for diagnostics or owner-specific cleanup. |
| `MarkReleasedAndUnregister` | Mark the handle as `Released` and remove it from the logical root registry. This is the policy needed before removing a scope root. |

## Idempotency

Double release is safe:

```text
Released -> release requested again -> SucceededAlreadyReleased
```

If the policy asks to unregister and the handle is already released, unregister is still attempted so a scope root can be cleared without destroying anything twice.

## Scope release

`ReleaseScopeLogically` snapshots handles from the logical root and releases each one through the same single-handle path.

It requires the root to exist. Missing root is not treated as success and no fallback root is created.

## What F8J does not do

F8J does not add:

```text
PrefabContentMaterializer
Scene materializer
Addressables materializer
Pooling materializer
GameObject
Transform
Instantiate
Destroy
Content Anchor binding
physical release execution
```

## Next

F8K should validate the runtime request/guard/release-policy loop and close F8 without introducing physical materialization inside the core.
