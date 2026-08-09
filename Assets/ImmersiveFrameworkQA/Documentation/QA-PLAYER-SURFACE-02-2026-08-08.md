# QA-PLAYER-SURFACE-02 — Negative / Stale Lifecycle Hardening

**Date:** 2026-08-08  
**Cut:** Q2 / `QA-PLAYER-SURFACE-02`  
**Runner:** `Immersive Framework/QA/Regressions/Player/Run QA-PLAYER-SURFACE-02 Public Surface Negative Regression`  
**Baseline:** Q1 `QaPlayerProvisioningPublicSurfaceRegression` (positive happy path)

---

## 1. Objective

Certify that the public Player consumer surface:

- rejects invalid commands with typed public results;
- does not mutate Session state on rejected operations;
- refuses missing / wrong / destroyed / replaced scopes without fallback;
- isolates Activity-owned evidence across exit/reentry/occurrences;
- preserves Session-owned join/Host when required by contract;
- does not present stale observation as current.

---

## 2. Case matrix

| Group | Cases |
|---|---|
| Commands | join closed; capacity exhausted; invalid capacity; open/close no-change |
| Scope P1 | missing binding; wrong scope; destroyed binding; stale endpoint after dispose/exit |
| Activity lifecycle | exit while WaitingForJoin; exit after join/select; reentry; newer occurrence; no duplicate Slot/Actor |
| Observation P2 | stale activity observation unavailable; old occurrence not current; revisions do not regress |
| Actor selection | stale selection revision rejected; repeated default selection stable |
| Public navigation | runtime-created unbound `ActivityRequestTrigger` fails explicitly; composition-time gap recorded |

---

## 3. Public APIs under test

- `ILocalPlayerProvisioningConsumerAccess` / `LocalPlayerProvisioningConsumerAccessBinding`
- `TryGetObservation` / immutable observation snapshots
- `OpenJoining` / `CloseJoining` / `SetDynamicCapacity` / `RequestJoin`
- `LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection`
- `PlayerProvisioningCommandTrigger` (missing binding validation/result)
- `ActivityRequestTrigger` (unbound failure diagnostics)

Arrangement only (not the Player contract path): readiness fixture Activity request/clear, M07 prepare, host readiness for environment.

---

## 4. Public navigation disposition

Runtime-created `ActivityRequestTrigger` instances are **not** composition-bound by Framework (binding occurs on Route/Activity/GlobalUI composition). Request fails with an explicit public unbound diagnostic.

```text
publicNavigation = gap-runtime-created-trigger-not-composition-bound
```

This is a **product reachability disposition**, not a privileged QA bypass. Full public end-to-end navigation requires an authored trigger present at composition time (Edit Mode fixture / product scene), not `TryBindActivityRuntime` from QA.

---

## 5. How to run

1. Exit Play Mode.  
2. Run M07 Prepare Internal Reconcile Regression.  
3. Enter a **fresh** Play Mode.  
4. Run Q2 menu item.  
5. Optional: run Q1 in another fresh Play Mode.  

Expect console:

```text
[QA_PLAYER_SURFACE_02] status='Passed' verdict='Q2_IMPLEMENTED_STATIC_OK' ...
```

Behavioral Unity certification remains separate until Play Mode is executed.

---

## 6. Suggested commit

```text
test(qa): harden public player surface lifecycle
```
