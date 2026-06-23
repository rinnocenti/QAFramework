# RuntimeContentHandle

Status: `F8I UPDATED`

`RuntimeContentHandle` is the passive canonical handle for one runtime-created content identity.

It exists to carry ownership and lifecycle/release state. F8D can register handles inside a logical runtime scope root. F8F connects those roots to lifecycle-owned contexts. F8G/F8H add request/result and transition guard contracts, but physical materialization adapters stay outside the core.

## Responsibilities

`RuntimeContentHandle` currently stores:

| Field | Role |
|---|---|
| `Identity` | Stable `RuntimeContentIdentity` composed from owner and content id. |
| `Owner` | Runtime content owner scope and typed owner identity. |
| `Scope` | Session, Route, Activity or Transient lifetime. |
| `ContentId` | Typed id for the runtime-created content record. |
| `State` | Passive lifecycle/release state. |
| `Source` / `Reason` / `Message` | Last transition diagnostics. |

## State operations

| Operation | Allowed source state | Result |
|---|---|---|
| `MarkMaterialized(source, reason)` | `Declared` | `Materialized` |
| `RequestRelease(source, reason)` | `Declared`, `Materialized`, `ReleaseFailed` | `ReleaseRequested` |
| `MarkReleased(source, reason)` | `ReleaseRequested` | `Released` |
| `MarkReleaseFailed(source, reason, failureMessage)` | `ReleaseRequested` | `ReleaseFailed` |

All operations return `RuntimeContentHandleTransitionResult`.

## Transition result status

| Status | Meaning |
|---|---|
| `Applied` | The handle state changed. |
| `IgnoredAlreadyInState` | The requested transition was already true or terminal. |
| `RejectedInvalidTransition` | The transition would violate the handle lifecycle. |

## F8D registry relation

`RuntimeContentHandle` remains passive. `RuntimeRootRegistry` can store handles by owner root and identity, but it does not alter handle state, create objects or release objects.

## Explicit non-goals

F8D does not add:

- runtime root GameObjects;
- GameObject references inside the handle;
- implementação de adapter físico;
- release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

F8E introduced the internal `RuntimeContentRuntime` owner and explicit `RuntimeScopeContext` boundary. F8F integrates logical runtime root/context creation and removal into Session, Route and Activity lifecycles. F8G/F8H add request/result and scoped cancellation. F8I adds `IRuntimeMaterializationAdapter` as the adapter boundary; physical adapters remain outside the core.

Next authorized cut:

```text
F8J — Runtime release policy / logical release execution
```
