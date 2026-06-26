# F20-ADR-PAUSE-002 - Pause State and Gate

Status: Accepted / In Progress through F20C  
Phase: F20 - Pause State and Pause Gate  
Type: Framework Core / Pause / Gate Consumer  
Last updated: 2026-06-26

---

## 1. Context

F10 recorded Pause as a consumer so it would not capture Route or Activity lifecycle.

F17 closed Gate Foundation. F18 closed Transition Orchestration. F19 closed Transition Effects as adapters/consumers. Pause can now start from the correct boundary: state plus Gate relationship, not overlay-first and not input-first.

Pause must not be reduced to a menu, overlay or `Time.timeScale`.

---

## 2. Decision

Pause is framework state plus Gate blocker relationship.

F20 owns only the logical Pause core:

```text
pause identity/state
pause request/result contracts
pause snapshot/facts
pause policy
Gate blocker relationship
diagnostics and QA smokes
```

Pause is not:

```text
Activity
Route lifecycle
Activity lifecycle
menu
overlay
input system
Time.timeScale contract
scene loading
transition effect
```

---

## 3. Operational Model

The canonical Pause flow is:

```text
pause request received
request admitted or rejected by policy
Pause state changes to Paused
Pause emits state/result/facts
Pause describes Gate blockers for gameplay/interaction scopes
allowed Pause-safe requests remain admissible
resume request received
Pause state changes to Running
Pause Gate blockers are released
Pause emits state/result/facts
```

The framework must keep this model independent from concrete overlay, input and timescale implementations.

---

## 4. Gate Relationship

Pause consumes Gate to block gameplay and interaction scopes while allowing explicitly permitted requests.

Allowed during Pause:

```text
resume
pause UI navigation
settings UI
diagnostic-safe framework requests
explicitly allowed transition requests
```

Generally blocked during Pause:

```text
gameplay mutation
gameplay input
world interaction
unapproved route/activity requests
unapproved object/cycle reset requests
```

F20 does not need a global Gate registry. Early cuts may describe and validate blockers passively, following the F18/F19 pattern.

---

## 5. Lifecycle Boundary

Pause does not own Route or Activity lifecycle.

Pause may coexist with Route/Activity state, but it must not model itself as an Activity or force a Route/Activity transition to pause gameplay.

Transition and Pause can interact, but neither replaces the other:

```text
Transition = flow orchestration around lifecycle/content/readiness/effects.
Pause = user/system state that gates gameplay and interaction while preserving controlled escape paths.
```

---

## 6. Time Scale Boundary

`Time.timeScale` is a possible future adapter or policy, not the central Pause contract.

The canonical contract must remain Pause state, Gate effects and explicit facts. A future time-scale adapter must be optional or required by policy and fail explicitly when required but absent.

---

## 7. Implementation Plan

| Cut | Status | Goal | Manual setup |
|---|---|---|---|
| F20A | `CLOSED / ADR PLAN ACCEPTED` | Accept Pause State/Gate boundary and implementation order. | None. Documentation only. |
| F20B | `CLOSED / PRIMITIVES APPLIED` | Add passive Pause primitives: state, request/result, reason/source, snapshot and issue/fact shape. | None. No scene/object/SO. |
| F20C | `CLOSED / DIAGNOSTICS SMOKE APPLIED` | Add synthetic Pause diagnostics smoke for request, pause applied, resume applied, toggle target, idempotent/no-change, rejected and snapshot cases. | None. No scene/object/SO/input/Gate/timeScale. |
| F20D | `PLANNED` | Add passive Pause-to-Gate blocker policy and smoke. | None expected. No runtime Gate registry. |
| F20E | `PLANNED` | Add minimal runtime Pause request path, likely through `FrameworkRuntimeHost`, without overlay/input ownership. | No saved scene setup expected unless the cut explicitly says otherwise. |
| F20F | `PLANNED` | Close F20 with Usage Guide and handoff to F21 Pause Content/Overlay/Input Boundary. | Usage guide only. |

---

## 8. Manual Setup Policy

F20A requires no scene, GameObject, Canvas, prefab or ScriptableObject.

F20 should stay mostly asset-free because it is the logical Pause core. If a later F20 cut unexpectedly needs a scene object or asset, that cut must document:

```text
which scene to open
which GameObject to create
which component to add
which fields to fill
which smoke to run
which logs prove PASS
```

Expected visual/content setup belongs to F21, not F20.

---

## 9. Excluded Now

F20 does not implement:

```text
Pause menu
Pause overlay content
pause input binding
Time.timeScale adapter
Pause modeled as Activity
Route/Activity lifecycle ownership
loading screen
fade/curtain ownership
gameplay contextual reset
```

---

## 10. Validation Strategy

F20 validation should follow the established sequence:

```text
compile/import pass
Standard Smoke for regression
synthetic Pause diagnostics smoke
synthetic Pause Gate blocker smoke
runtime request smoke only when the request path exists
```

Negative evidence should be explicit when required behavior is missing, especially for invalid request, duplicate request, blocked request and required policy absence.

---

## 11. Guardrails

- Pause is state + Gate blocker relationship.
- Pause is not Activity.
- Pause does not control Route/Activity lifecycle.
- Pause blocks gameplay/interaction, but does not necessarily block UI, resume or allowed diagnostics.
- Pause must not use service locator/global singleton as the canonical API.
- Pause must not rely on `Time.timeScale` as the framework contract.
- Pause visual content and input are F21 boundaries.

---

## 12. Handoff

F20 hands off to F21 only after Pause state, Pause diagnostics and Pause Gate relationship are closed and documented in a Usage guide.

F21 then owns Pause content/overlay/input as consumers of the F20 logical contract.

---

## 13. F20B Applied Primitives

F20B adds the passive Pause language under `Runtime/Pause`:

```text
PauseRequestId
PauseState
PauseRequestKind
PauseRequestStatus
PauseIssueSeverity
PauseIssue
PauseRequest
PauseResult
PauseSnapshot
```

These types are data/diagnostics only. They do not read input, open menu content, set `Time.timeScale`, execute Gate blockers, own Route/Activity lifecycle or create a Pause runtime owner.

`FrameworkIdentityDomain.Pause = 160` is reserved for Pause request identity.

F20C validates these shapes through a synthetic QA smoke before a runtime request path is introduced.


---

## 14. F20C Diagnostics Smoke

F20C adds the synthetic QA smoke:

```text
Run Pause Diagnostics Smoke
```

The smoke is exposed under:

```text
Show Pause diagnostics
```

Validated steps:

```text
request
pause-applied-result
resume-applied-result
toggle-target-state
ignored-no-change-result
rejected-result
snapshot
```

The smoke validates passive Pause shapes only. It does not:

```text
read input
open overlay/menu content
execute a runtime Pause request path
register a real Gate blocker
change Time.timeScale
change Route/Activity lifecycle
create scene objects, Canvas, prefab or ScriptableObject assets
```

F20D is the next cut and should define the passive Pause-to-Gate blocker relationship before a real runtime request path is introduced.
