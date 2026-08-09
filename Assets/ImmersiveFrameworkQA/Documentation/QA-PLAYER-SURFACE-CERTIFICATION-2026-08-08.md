# Player Surface QA — Joint Certification

**Original runbook date:** 2026-08-08  
**Certified:** 2026-08-09  
**Cuts:** QA-PLAYER-SURFACE-01 + QA-PLAYER-SURFACE-02  
**Status:** **PLAYER SURFACE QA CERTIFIED**

---

## Certified result

```text
Q1 public positive proof
  PASS — 29/29

Q2 negative / stale-scope / lifecycle hardening
  PASS — 36/36

Authored public navigation
  PASS

Final orchestrator verdict
  PLAYER SURFACE QA CERTIFIED
```

This certification is Unity Play Mode behavioral evidence, not static-only implementation status.

---

## One-shot Unity certification

### A. Edit Mode prepare

1. Exit Play Mode.
2. Menu: **Immersive Framework/QA/Setup/Player/Prepare Player Surface Full Certification**.

The prepare step reuses the canonical M07 Player fixture and authors the Player Surface public navigation/binding fixture before Play Mode.

Expected preparation includes:

```text
M07 canonical Player fixture prepared/reused
QA_Hub public navigation root authored
Route LocalPlayerProvisioningConsumerAccessBinding authored
Enter/Clear ActivityRequestTrigger authored
WaitCovered Player Surface Activity authored
QA_UIGlobal local QA fixture prepared
source QA_UIGlobal closed before Play Mode
```

### B. Automated joint run

Menu: **Immersive Framework/QA/Regressions/Player/Run Player Surface Full Certification (Q1+Q2)**

The orchestrator executes:

```text
prepare
→ fresh Play Mode Q1
→ Q1 PASS
→ exit Play Mode / restore
→ re-prepare canonical fixtures
→ fresh Play Mode Q2
→ Q2 PASS
→ Complete / PLAYER SURFACE QA CERTIFIED
```

Q2 does not inherit a dirty Q1 Session.

---

## Authored public navigation

Happy-path navigation is composition-time authored:

```text
QA_Hub
  QA_PlayerSurface_PublicNavigation
    Enter ActivityRequestTrigger
    Clear ActivityRequestTrigger
    Route consumer binding
```

Framework composition binds the authored trigger/binding. Q1 does not use privileged QA `TryBind` or internal Activity ports as its operational path.

A runtime-created unbound `ActivityRequestTrigger` remains a deliberate Q2 negative and is expected to fail explicitly.

---

## Persistent Global UI fixture rule

`QA_UIGlobal` is prepared as authored source content, then closed before Play Mode. The Framework loads/persists the canonical Global UI composition once.

Q1/Q2 may resolve the QA-owned runtime fixture deterministically, but they do not discover `LocalPlayerActorSelectionRequestAuthoring` or other Player authority directly through scene/global scans.

The fixture points to the local authored public Actor-selection component.

---

## Certification boundaries

### Proved publicly

```text
P1 scoped consumer access
P2 immutable observation
Open / Close Joining
Set Dynamic Capacity
Request Join
public default Actor selection
Slot / Host / Actor lifecycle observation
WaitingForJoin + WaitCovered pending then terminal
normal preparation / physical materialization / gameplay admission outcome
Activity exit / Session persistence
reentry with newer occurrence and no duplicate Slot/Actor
negative command semantics
wrong/missing/stale/destroyed scopes
stale Actor revision
```

### Still internal QA responsibilities

```text
reservation mutation
assignment token/owner/origin mutation
internal prepare/materialize/admit commands
reconcile / rollback authority internals
runtime module lookup
```

The certification does not justify making these internal operations public.

---

## Expected negative logs

Q2 intentionally emits some framework error diagnostics. Treat individual error records according to the case being tested. The certification source of truth is the typed runner result plus final orchestrator verdict.

---

## Final certified verdict

```text
[QA_PLAYER_SURFACE_CERT]
status='Complete'
navigation='PASS'
q1='PASS'
q2='PASS'
verdict='PLAYER SURFACE QA CERTIFIED'
```

Reopen this certification only if new evidence contradicts the public Player Surface contract or a later package change invalidates the certified lifecycle.
