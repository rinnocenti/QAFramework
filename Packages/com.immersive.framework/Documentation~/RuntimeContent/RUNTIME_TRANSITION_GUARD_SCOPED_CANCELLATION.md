# Runtime Transition Guard and Scoped Cancellation

Status: `F8H APPLIED / GUARDRAILS`

This document records the F8H transition guard added after runtime ownership primitives, passive handles, logical roots, `RuntimeContentRuntime`, lifecycle root integration and materialization request/result contracts.

F8H does not materialize anything by itself. It adds the guardrails required before a adapter físico can safely instantiate runtime content.

---

## Problem

Before F8H, a `RuntimeMaterializationRequest` could describe owner, content id and resource, but it had no token proving that the owner scope was still current.

That is unsafe for later async or multi-step materializers:

```text
Route A / Activity X creates request
Route or Activity starts exiting
materializer finishes later
old request registers content into stale scope
```

F8H prevents this by adding a runtime-local transition guard and scoped cancellation token.

---

## Added runtime concepts

| Type | Role |
|---|---|
| `RuntimeScopeTransitionState` | Public experimental state vocabulary: `Active`, `CancellationRequested`, `Removed`. |
| `RuntimeScopeCancellationToken` | Public experimental immutable token captured by `RuntimeMaterializationRequest`. |
| `RuntimeScopeTransitionGuard` | Internal runtime-local owner/version guard. |
| `RuntimeScopeTransitionGuardStatus` | Internal diagnostic status vocabulary for guard decisions. |
| `RuntimeScopeTransitionGuardResult` | Internal diagnostic result for guard operations. |

---

## Lifecycle behavior

The guard is owned by `RuntimeContentRuntime`.

```text
RuntimeContentRuntime
  -> RuntimeRootRegistry
  -> RuntimeScopeTransitionGuard
```

When a scope root is created, the guard opens the scope as active.

When a scope root is removed, the guard first requests cancellation for that scope. If the root is actually removed or already missing, the guard marks that scope as removed.

If root removal is rejected because registered handles still exist, the scope remains cancellation-requested. New materialization is rejected until the lifecycle/release path resolves the handles in a later F8 cut.

---

## Materialization request behavior

`RuntimeMaterializationRequest` now carries:

```text
RuntimeScopeContext
RuntimeContentId
RuntimeMaterializationResource
RuntimeScopeCancellationToken
source/reason
```

`RuntimeContentRuntime.CreateMaterializationRequest(...)` now asks the guard whether materialization is allowed before creating the request.

If the scope is cancelling, removed or missing, request creation is rejected before any adapter físico can run.

F8H also adds `RuntimeContentRuntime.IsMaterializationRequestCurrent(...)` so a future materializer can validate the request again before and after side effects.

---

## New materialization statuses

`RuntimeMaterializationStatus` now includes:

| Status | Meaning |
|---|---|
| `RejectedScopeTransition` | The scope was not active when the request was created or consumed. |
| `RejectedScopeCancellation` | The scope started exiting and requested cancellation. |
| `RejectedStaleScope` | The request token is older than the current owner scope version. |

---

## What F8H does not do

F8H does not add:

- adapter físico;
- materialização física runtime;
- hierarchy root `GameObject`;
- `Transform` parenting;
- `Instantiate`;
- `Destroy`;
- physical release execution;
- async scheduling;
- `Task.Delay`;
- background work;
- Content Anchor binding;
- Actor/Pause/Camera/UI/Input/Save/Pooling consumer.

---

## Next authorized cut

```text
F8J — Runtime release policy / logical release execution [APPLIED / PENDING COMPILE-SMOKE]
F8K — Runtime request/guard/release-policy smoke and F8 closure
```
