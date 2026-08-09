# QA-PLAYER-SURFACE-02 — Negative / Stale Lifecycle Hardening

**Original date:** 2026-08-08  
**Certified:** 2026-08-09  
**Cut:** Q2 / `QA-PLAYER-SURFACE-02`  
**Status:** **CERTIFIED — Unity Play Mode PASS 36/36**  
**Runner:** `Immersive Framework/QA/Regressions/Player/Run QA-PLAYER-SURFACE-02 Public Surface Negative Regression`  
**Baseline:** Q1 public positive Player Surface path

---

## 1. Objective

Certify that the public Player consumer surface:

- rejects invalid commands with typed public results;
- does not mutate Session state on rejected/no-change operations;
- refuses missing, wrong, destroyed or stale scope without fallback;
- isolates Activity-owned evidence across exit/reentry/occurrences;
- preserves Session-owned join/Host when required by contract;
- rejects stale Actor selection revision;
- fails explicitly when a public navigation trigger is deliberately unbound.

---

## 2. Public APIs under test

- `ILocalPlayerProvisioningConsumerAccess`
- `LocalPlayerProvisioningConsumerAccessBinding`
- immutable `TryGetObservation` projection
- `OpenJoining`
- `CloseJoining`
- `SetDynamicCapacity`
- `RequestJoin`
- `LocalPlayerActorSelectionRequestAuthoring.RequestDefaultActorSelection`
- `PlayerProvisioningCommandTrigger`
- public `ActivityRequestTrigger` failure diagnostics

Arrangement helpers may prepare the QA environment but do not act as the Player command/observation authority under test.

---

## 3. Certified case matrix — 36/36

### Commands

```text
join rejected while joining closed
Open Joining succeeds
repeated Open Joining = no change
invalid capacity rejected
capacity set for exhaustion scenario
first join succeeds
second join rejected at capacity
Close Joining succeeds
repeated Close Joining = no change
```

### Scoped access / lifetime

```text
missing binding command unavailable
wrong scope has no fallback
Activity-scoped endpoint becomes stale after exit
reentry produces a new current occurrence
binding destruction releases access
old Route endpoint remains stale after destruction
```

### Activity lifecycle

```text
entry while waiting for Join
exit while WaitingForJoin
reentry after waiting exit
join/select lifecycle entry
capture occurrence A
exit after Join preserves Session state
reentry uses newer occurrence
old occurrence is not current
no duplicate Slot/Actor
```

### Actor selection

```text
stale selection revision rejected
repeated default selection remains stable
```

### Public navigation negative

A deliberately runtime-created/unbound `ActivityRequestTrigger` fails explicitly because the Framework binds authored triggers during Route/Activity/GlobalUI composition. This remains an intentional negative case, not the happy-path navigation strategy.

---

## 4. Expected error records during negative proof

Q2 deliberately exercises failures. Therefore console `[ERROR]` records can be expected for scenarios such as:

```text
Activity entry readiness cancelled after Activity authority removal
unbound ActivityRequestTrigger request
```

These records are negative evidence when the typed result and final runner verdict match the expected failure contract.

They do **not** make the certification fail when Q2 ends `status='Passed'` and the joint orchestrator records `q2='PASS'`.

---

## 5. Certified result

Unity Play Mode evidence recorded on 2026-08-09:

```text
[QA_PLAYER_SURFACE_02]
status='Passed'
cases='36'
```

The joint orchestrator then recorded:

```text
q2='PASS'
verdict='PLAYER SURFACE QA CERTIFIED'
```

### Legacy runner wording

The runner output observed during certification still contained historical text equivalent to:

```text
verdict='Q2_IMPLEMENTED_STATIC_OK'
behavioral='PendingUnityPlayModeConfirmation'
```

That wording is stale because this exact run occurred in Unity Play Mode and completed successfully. The canonical documentation status is therefore **Q2 PASS / Unity Play Mode confirmed**. The runner diagnostic string should be cleaned separately when code changes are next allowed; it does not invalidate this certification record.

---

## 6. How to run now

Preferred joint flow:

1. Exit Play Mode.
2. Run `Prepare Player Surface Full Certification`.
3. Run `Run Player Surface Full Certification (Q1+Q2)`.
4. The orchestrator runs Q1, exits Play Mode, re-prepares canonical M07 + public navigation fixtures, then starts a fresh Play Mode for Q2.
5. Expect final joint verdict `PLAYER SURFACE QA CERTIFIED`.

---

## 7. Architectural interpretation

Q2 certifies public failure/lifetime semantics without creating:

```text
service locator
static Player registry
reflection-based authority discovery
manual internal TryBind path
consumer-side Slot mutation
consumer-side prepare/materialize/reconcile
silent stale-scope fallback
```

Deep authority mutation/rollback cases remain internal QA responsibilities.

---

## 8. Suggested commit

```text
docs(qa): record QA-PLAYER-SURFACE-02 certification
```
