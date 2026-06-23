# Runtime Ownership Primitives

Status: `F8F UPDATED`

F8B introduced passive primitives for runtime-created content ownership. F8C added a passive `RuntimeContentHandle` that records lifecycle/release state transitions without executing materialization or release. F8D added logical scope roots and an internal minimal registry for explicit root/handle registration. F8F connects those roots to Session, Route and Activity lifecycle context creation/removal.

## Added runtime primitives

| Primitive | Role |
|---|---|
| `RuntimeContentScope` | Declares the owner lifetime: `Session`, `Route`, `Activity` or `Transient`. |
| `RuntimeContentState` | Defines passive state vocabulary for future handles and release diagnostics. |
| `RuntimeContentId` | Typed id for one runtime-created content record. It is not a GameObject name or prefab path. |
| `RuntimeContentOwner` | Couples a runtime content scope with a typed owner identity. |
| `RuntimeContentIdentity` | Combines owner and content id into a stable runtime content identity. |
| `RuntimeContentHandle` | Passive canonical handle for one runtime-created content identity. |
| `RuntimeContentHandleTransitionStatus` | Result vocabulary for passive handle state transitions. |
| `RuntimeContentHandleTransitionResult` | Immutable diagnostic result for one handle transition. |
| `RuntimeScopeRoot` | Internal logical root for one runtime content owner. |
| `RuntimeRootRegistry` | Internal scoped registry for runtime roots and handle registration. |
| `RuntimeRootRegistryOperationStatus` | Internal status vocabulary for root registry operations. |
| `RuntimeRootRegistryOperationResult` | Internal diagnostic result for root registry operations. |

## Scope to owner domain

F8B keeps ownership strict:

| Runtime scope | Expected owner identity domain |
|---|---|
| `Session` | `FrameworkIdentityDomain.Session` |
| `Route` | `FrameworkIdentityDomain.Route` |
| `Activity` | `FrameworkIdentityDomain.Activity` |
| `Transient` | `FrameworkIdentityDomain.Runtime` |

A mismatched owner domain throws at construction time. This prevents silent fallback roots and avoids using GameObject names as ownership identity.

## RuntimeContentHandle state transitions

F8C allows a handle to record these passive transitions:

| Operation | Allowed source state | Target state | Notes |
|---|---|---|---|
| `MarkMaterialized` | `Declared` | `Materialized` | Used later by materializers after a concrete instance exists. |
| `RequestRelease` | `Declared`, `Materialized`, `ReleaseFailed` | `ReleaseRequested` | Records intent only. No object is destroyed or returned to a pool. |
| `MarkReleased` | `ReleaseRequested` | `Released` | Used later by release execution after successful side effect. |
| `MarkReleaseFailed` | `ReleaseRequested` | `ReleaseFailed` | Records failed release evidence only. |

Repeated requests that are already in the target state produce `IgnoredAlreadyInState`. Invalid lifecycle jumps produce `RejectedInvalidTransition`.

## Root registry baseline

F8D keeps runtime roots logical/passive. `RuntimeRootRegistry` can create a root explicitly and can register handles only when the owner root already exists. Registering a handle without a root returns `RejectedMissingRoot`; it does not create a fallback root.

## Explicit non-goals

F8D does not add:

- runtime scope root GameObjects;
- materialization request/result;
- prefab materializer;
- release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

F8E introduced the internal `RuntimeContentRuntime` owner and explicit `RuntimeScopeContext` boundary. F8F now integrates logical runtime root/context creation and removal into Session, Route and Activity lifecycles.

Next authorized cut:

```text
F8G — RuntimeMaterializationRequest / RuntimeMaterializationResult
```
