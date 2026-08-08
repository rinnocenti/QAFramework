# QA-PLAYER-SURFACE-01 — Public-only Player Contract Proof

**Date:** 2026-08-08  
**Cut:** Q1 / `QA-PLAYER-SURFACE-01`  
**Status:** Implemented in QAFramework (Play Mode certification required)  
**Runner:** `Immersive Framework/QA/Regressions/Player/Run QA-PLAYER-SURFACE-01 Public Provisioning Surface Regression`

---

## 1. Objective

Prove a real Manager-Provisioned Player lifecycle using only public consumer surfaces for the Player contract under test:

```text
scoped access (P1)
→ public commands
→ public observation (P2)
→ public Actor selection
→ normal runtime preparation / materialization / admission
→ readiness / WaitCovered / loading / gate
→ Activity exit preserves Session Host/join
→ reentry without duplicate Slot/Actor
```

If preparation/materialization/admission does not complete after public Join + default Actor selection, Q1 fails as a **package gap** (no QA bypass).

---

## 2. Documents investigated

| Document | Role |
|---|---|
| IF-ADR-015 | Command/observation consumer boundary |
| IF-ADR-016 | PlayerSessionProfile / ProvisioningProfile initialization |
| IF-ADR-015 Closure Plan | P1–P4 / Q1 sequence and allowed surfaces |
| IF-PLAYER-SURFACE-01E2 Public Contract Coverage Audit | Public vs internal coverage matrix |
| Player Session / Activity reconciliations | Ownership of Host/join vs Activity projection |
| Transversal invariants / decision classification | Authority separation |
| P1–P4 package implementations | Consumer access, observation, command trigger, status binding |

---

## 3. Existing QA investigated / reused

| Asset | Reuse |
|---|---|
| `QaManagerProvisionedLifecyclePublicContractRegression` | Edit Mode contract shape (kept) |
| `QaManagerProvisionedLifecycleWaitingProjectionRegression` | WaitingForJoin / Released projection pattern |
| `QaM07ActivitySessionLifecycleProjectionRegression` | Exit preserves Session; occurrence switch |
| `QaPlayerActorSelectionRuntimeBindingRegression` | Public default Actor selection |
| `QaParticipantAwareReadinessLoadingProgressRegression` | WaitCovered loading grammar (internal) |
| `QaM07InternalReconcileSetup` | Edit Mode environment preparation only |
| `QaActivityEntryReadinessFixture` | Temporary WaitCovered Activity arrangement |

---

## 4. Canonical QA structure reused

```text
Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/
  QaPlayerProvisioningPublicSurfaceRegression.cs

Assets/ImmersiveFrameworkQA/Documentation/
  QA-PLAYER-SURFACE-01-2026-08-08.md
```

No parallel harness, no new package API, no ZIP, no Git operations.

---

## 5. Public APIs exercised

| Concern | Public surface |
|---|---|
| Scoped access | `LocalPlayerProvisioningConsumerAccessBinding` + `ILocalPlayerProvisioningConsumerAccess` |
| Observation | `TryGetObservation` → `LocalPlayerProvisioningConsumerObservationSnapshot` |
| Joining / Capacity / Join | `OpenJoining`, `SetDynamicCapacity`, `RequestJoin` |
| Actor selection | `LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection` |
| Lifecycle evidence | `ManagerProvisionedPlayerLifecycleSnapshot` via consumer observation |
| Slot/Host/Actor readiness | `LocalPlayerProvisioningConsumerSlotObservation` |
| Loading presentation | `QaLoadingSurfaceVisibilityHoldAdapter` (product-visible loading surface already in UIGlobal) |
| Authoring fixtures | `GameApplicationAsset`, `PlayerSlotProfile`, `ActorProfile`, `ActivityAsset`, Route content scene |

**Not used as consumer path**

```text
reflection
FindObjectOfType / FindObjectsByType authority lookup
FrameworkRuntimeHost module GetComponent for Player authority
PrepareSelectedActor / EnsureGameplayReady / reconcile
external Slot mutation
log parsing
```

**Arrangement only (not the Player consumer path under test)**

```text
QaActivityEntryReadinessFixture.Activities.RequestActivityAsync / ClearActivityAsync
host resolution for Game Flow readiness environment
```

---

## 6. Assertions (typed evidence)

- scoped access available (`IsBound` / `Snapshot.IsAvailable`)
- Joining open after public OpenJoining
- Dynamic capacity applied after public SetDynamicCapacity
- Slot joined + Host evidence after public RequestJoin
- selected Actor after public default selection
- prepared / materialized / gameplay admitted without privileged QA APIs
- lifecycle Ready + GateHeld false
- Session revision + Applied Session revision correlation
- Activity occurrence present
- WaitCovered pending before Player Ready (request incomplete + gate held + loading not terminal)
- loading/gate terminal after Ready
- Activity-owned projection released on exit
- Session-owned Host/join persists
- reentry newer occurrence, no duplicate Slot/Actor
- CloseJoining
- static source scan for forbidden tokens

---

## 7. How to run

1. Exit Play Mode.
2. Run `Immersive Framework/QA/Setup/Player/M07 Prepare Internal Reconcile Regression`.
3. Enter a **fresh** Play Mode (no previously joined Players).
4. Run `Immersive Framework/QA/Regressions/Player/Run QA-PLAYER-SURFACE-01 Public Provisioning Surface Regression`.
5. Expect console:
   - `status='Passed' verdict='Q1_PASS'` or
   - `status='Failed' verdict='Q1_FAIL'` with package-gap wording if normal lifecycle does not advance.

Unity/Play Mode not executable in CI batch must be recorded as **pending behavioral certification**, not PASS.

---

## 8. Expected package gap report format (if FAIL on lifecycle)

1. Where the public flow stopped  
2. Public state observed  
3. Expected operation  
4. Probable architectural cause  
5. Smallest package fix  

---

## 9. Q2 candidates

- joining closed reject
- capacity exhausted / invalid capacity
- stale Actor selection revision
- unavailable / wrong / disposed consumer scope
- exit while WaitingForJoin / mid progression
- pure ActivityRequestTrigger-only entry without readiness fixture arrangement
- repeated no-change operations

---

## 10. Suggested commit message

```text
test(qa): prove public player provisioning surface (QA-PLAYER-SURFACE-01)
```
