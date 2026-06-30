# F9R-R — Route / Activity Exit Auto-Release Decision

Status: Accepted / Decision / docs-only

Scope: architecture decision only. This cut does not change runtime code, does not add lifecycle wiring and does not authorize Route/Activity auto-materialization.

## Purpose

F9R-R decides whether the framework can wire lifecycle-owned release execution into Route or Activity exit after the F9R-N/O/P/Q proofs.

The short decision is:

```text
Route/Activity exit auto-release is not approved yet.
Explicit lifecycle release execution is proven, but lifecycle exit wiring still needs a composite release path that covers logical RuntimeContent release, ContentAnchor binding cleanup and physical Unity adapter release evidence together.
```

## Evidence Reviewed

| Cut | Evidence carried into F9R-R |
|---|---|
| F9R-N | A lifecycle-owned materialization registry can register typed materialization evidence and track entry state. |
| F9R-O | Explicit bridge/bridge set materialization handles can be registered into the lifecycle registry. |
| F9R-P | The lifecycle registry can create query-only release plans by owner or scope. |
| F9R-Q | The lifecycle registry can execute a release plan explicitly through a caller-provided `RuntimeReleaseRequest` executor and mirror the result into registry state. |

## Decision

| Question | Decision |
|---|---|
| Can Route/Activity exit call lifecycle registry auto-release now? | No. |
| Can auto-release be pursued before auto-materialization? | Yes, but only after a composite release path is proven. |
| Can Route/Activity auto-materialize content? | No. Still blocked. |
| Can consumers be unlocked by F9R-Q alone? | No. |
| Can explicit QA/authored release continue using bridge/bridge set release? | Yes. |

## Why Auto-Release Is Rejected For Immediate Wiring

F9R-Q proves explicit logical release execution through a delegated runtime release executor. It intentionally reports:

```text
physicalRelease='False'
contentAnchorBindingCleanup='False'
routeActivityAutoRelease='False'
```

That is correct for F9R-Q, but insufficient for Route/Activity exit.

A lifecycle exit must not report release success while leaving any of these unclear:

| Layer | Required before lifecycle auto-release |
|---|---|
| RuntimeContent | Logical handles released or failed explicitly. |
| ContentAnchor binding | Logical bindings removed or preserved with an explicit reason. |
| Unity physical object | Physical release requested through the owning adapter/bridge/registry. |
| Lifecycle registry | Entry state updated to `Released` or `ReleaseFailed`. |
| Diagnostics | Aggregate result exposes counts and blocking failures. |
| Re-entry/idempotency | Repeated release/exit does not double-release or hide failures. |

## Accepted Rule

```text
auto-release may come before auto-materialization
but only for explicitly registered lifecycle-owned materialization entries
and only after composite release is proven
```

## Required Future Shape Before Route/Activity Wiring

A future implementation must provide an explicit composite release path with this order:

```text
freeze lifecycle owner identity
  -> create lifecycle materialization release plan
  -> execute physical adapter/bridge release for each owned entry
  -> cleanup ContentAnchor logical binding for released entries
  -> execute RuntimeContent logical release
  -> update lifecycle materialization registry state
  -> report aggregate lifecycle release result
  -> only then allow Route/Activity exit to complete as released
```

A later ADR/cut may change the exact order, but it must explain why and preserve equivalent evidence.

## Blocking Requirements For Future Auto-Release

| Requirement | Status after F9R-R |
|---|---|
| Lifecycle registry contract | Met by F9R-N. |
| Bridge-created handle registration | Met by F9R-O. |
| Owner/scope release plan | Met by F9R-P. |
| Explicit logical release execution | Met by F9R-Q. |
| Physical adapter/bridge release execution from lifecycle path | Missing. |
| ContentAnchor binding cleanup from lifecycle path | Missing. |
| Aggregate lifecycle release result for Route/Activity exit | Missing. |
| Route/Activity exit integration | Rejected until missing requirements are proven. |

## Non-goals

F9R-R does not implement:

- runtime code;
- editor code;
- scene, prefab or asset changes;
- Route/Activity auto-release;
- Route/Activity auto-materialization;
- lifecycle runtime wiring;
- physical release from lifecycle registry;
- ContentAnchor binding cleanup from lifecycle registry;
- Pause consumer;
- Camera consumer;
- Audio consumer;
- Save/progression consumer;
- Actor materialization;
- Pooling/runtime-spawned integration;
- PlayerJoin;
- F34/gameplay.

## Consumer Unlock Status

| Consumer / track | Status after F9R-R |
|---|---|
| Pause ContentAnchor consumer | Still blocked. |
| Camera | Still blocked. |
| Audio | Still blocked. |
| Save / Snapshot / Progression | Still blocked from physical object lifetime assumptions. |
| Actor materialization | Still blocked. |
| Pooling / runtime-spawned | Still blocked. |
| Player join | Still blocked. |
| F34 / gameplay | Still blocked. |

## Candidate Next Cut

The next safe cut is not Route/Activity wiring.

Recommended next cut:

```text
F9R-S — Explicit Composite Lifecycle Release Executor Proof
```

Purpose:

```text
prove one explicit executor path that can release lifecycle-owned materialization entries across:
- physical adapter/bridge release evidence;
- ContentAnchor binding cleanup evidence;
- RuntimeContent logical release evidence;
- lifecycle registry state update evidence.
```

Still blocked in F9R-S unless explicitly changed later:

```text
Route/Activity auto-release
Route/Activity auto-materialization
Pause / Camera / Audio / Save / Actor / Pooling / PlayerJoin / gameplay
```

## Decision

F9R-R is accepted as a docs-only decision.

It rejects immediate Route/Activity exit auto-release and selects the composite release gap as the next hardening target.
