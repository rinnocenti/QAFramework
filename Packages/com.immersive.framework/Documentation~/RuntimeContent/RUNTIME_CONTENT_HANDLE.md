# RuntimeContentHandle

Status: `F8C APPLIED`

`RuntimeContentHandle` is the passive canonical handle for one runtime-created content identity.

It exists to carry ownership and lifecycle/release state before F8 introduces roots, registry, materialization requests, prefab materialization and release execution.

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

## Explicit non-goals

F8C does not add:

- runtime root GameObjects;
- root registry;
- GameObject references inside the handle;
- materialization request/result;
- prefab materializer;
- release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8D — RuntimeScopeRoot + internal minimal registry
```
