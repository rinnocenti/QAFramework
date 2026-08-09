# QA-PLAYER-SURFACE-01 — Public-only Player Contract Proof

**Original date:** 2026-08-08  
**Certified:** 2026-08-09  
**Cut:** Q1 / `QA-PLAYER-SURFACE-01`  
**Status:** **CERTIFIED — Unity Play Mode PASS 29/29**  
**Runner:** `Immersive Framework/QA/Regressions/Player/Run QA-PLAYER-SURFACE-01 Public Provisioning Surface Regression`

---

## 1. Objective

Prove a real Manager-Provisioned Player lifecycle through the official public consumer surface:

```text
authored public Activity navigation
→ scoped Player provisioning access (P1)
→ public commands
→ immutable observation (P2)
→ public default Actor selection
→ normal runtime preparation / materialization / admission
→ readiness / WaitCovered / loading / gate
→ Activity exit preserves Session-owned Host/join
→ reentry with a newer Activity occurrence and no duplicate Slot/Actor
```

Q1 does not call internal Player preparation, materialization, admission or reconcile operations to make the flow pass.

---

## 2. Package contracts under test

| Concern | Public surface |
|---|---|
| Scoped access | `LocalPlayerProvisioningConsumerAccessBinding` + `ILocalPlayerProvisioningConsumerAccess` |
| Observation | `TryGetObservation` → immutable `LocalPlayerProvisioningConsumerObservationSnapshot` |
| Joining / Capacity / Join | `OpenJoining`, `SetDynamicCapacity`, `RequestJoin`, `CloseJoining` |
| Actor selection | `LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection` |
| Lifecycle evidence | `ManagerProvisionedPlayerLifecycleSnapshot` through consumer observation |
| Slot evidence | joined state, Host, selected Actor, preparation/materialization/admission evidence |
| Activity correlation | Activity owner/occurrence + Session/applied revision evidence |
| Public navigation | authored composition-time `ActivityRequestTrigger` |

Forbidden as the Player consumer path:

```text
reflection
FindObjectOfType / FindObjectsByType authority discovery
FrameworkRuntimeHost Player module lookup
TryBind... authority shortcuts
PrepareSelectedActor
EnsureGameplayReady
reconcile
external Slot mutation
log parsing as authority
runtime-created replacement consumer binding
```

---

## 3. Canonical arrangement

Preparation reuses the existing canonical M07/Player fixtures and authors the public navigation/binding before Play Mode.

```text
QA_Hub
  QA_PlayerSurface_PublicNavigation
    Enter ActivityRequestTrigger
    Clear ActivityRequestTrigger
    Route LocalPlayerProvisioningConsumerAccessBinding

QA_UIGlobal
  canonical persistent Player provisioning composition
  QaPlayerSurfaceGlobalUiFixture
    explicit local Actor Selection reference
```

`QA_UIGlobal` is not left open as an extra source-scene copy before Play Mode. The Framework loads/persists the canonical Global UI composition once.

Q1 resolves QA-owned fixtures deterministically and consumes their explicit references. It does not discover framework Player authorities by scene scan.

---

## 4. Certified assertions — 29/29

The certified runner completed all cases:

```text
play-mode-required
setup-confirmed
runtime-started
public-navigation-fixture-resolved
public-activity-trigger-composition-bound
consumer-binding-authored
scoped-access-available
fresh-session-confirmed
waitcovered-activity-configured
activity-entry-started
waiting-for-join-observed
waitcovered-loading-pending
joining-opened
dynamic-capacity-set
public-join-succeeded
joined-slot-host-observed
default-actor-selection-requested
selected-actor-observed
normal-lifecycle-ready
prepared-materialized-admitted
waitcovered-loading-terminal
activity-entry-completed
activity-exit-released
session-host-persists
reentry-newer-occurrence
reentry-no-duplicate-slot-actor
joining-closed
fixture-cleaned
public-scan-clean
```

The certification specifically closes the previously open public `WaitingForJoin + WaitCovered` proof: loading remains non-terminal while the required Player is unresolved, then completes only after the public Join/Actor lifecycle reaches Ready.

---

## 5. Certified result

Unity Play Mode evidence recorded on 2026-08-09:

```text
[QA_PLAYER_SURFACE_01]
status='Passed'
verdict='Q1_PASS'
cases='29'
```

Observed correlation in the certified run included distinct Activity occurrences for first entry/reentry and no duplicate Slot/Actor materialization.

No package runtime gap remained after the QA fixture/order corrections.

---

## 6. How to run now

Preferred joint certification:

1. Exit Play Mode.
2. Run `Immersive Framework/QA/Setup/Player/Prepare Player Surface Full Certification`.
3. Confirm the authored preparation completes.
4. Run `Immersive Framework/QA/Regressions/Player/Run Player Surface Full Certification (Q1+Q2)`.
5. Expect Q1 `verdict='Q1_PASS'` before the orchestrator exits/re-prepares for Q2.

Q1 can still be run separately in a fresh prepared Play Mode when isolating the positive path.

---

## 7. Certification interpretation

This is a **public consumer contract certification**, not a replacement for internal authority QA.

Internal tests should continue proving reservation ownership, assignment-token invariants, rollback, reconciliation and other privileged authority behavior. Those internals must not be promoted into public APIs merely for Q1 convenience.

---

## 8. Suggested commit message

```text
docs(qa): record QA-PLAYER-SURFACE-01 certification
```
