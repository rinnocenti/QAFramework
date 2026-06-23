# RuntimeContentHandle

Status: `F8D APPLIED`

`RuntimeContentHandle` is the passive canonical handle for one runtime-created content identity.

It exists to carry ownership and lifecycle/release state. F8D can register handles inside a logical runtime scope root, but materialization requests, prefab materialization and release execution still come later.

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

## F8D1 plan realignment

F8D1 changes the next gate after this document. The next authorized technical cut is not materialization request/result. The next gate is:

```text
F8E — RuntimeContentRuntime + RuntimeScopeContext
```

Request/result, prefab materialization and release execution remain in F8, but only after the internal runtime content owner, scope context and lifecycle root integration exist.

## Explicit non-goals

F8D does not add:

- runtime root GameObjects;
- GameObject references inside the handle;
- materialization request/result;
- prefab materializer;
- release execution;
- Content Anchor binding;
- Actor, Pause, Camera, UI, Input, Save or Pooling consumers.

Next authorized cut:

```text
F8E — RuntimeContentRuntime + RuntimeScopeContext
```
