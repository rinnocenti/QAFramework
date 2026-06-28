# F27 Plan — Pause UIGlobal Surface and Input Wiring

## Status

Frozen after F27D / runtime implementation paused

## Purpose

F27 turned the logical Pause foundation into a minimal Unity-facing surface and validated a narrow PauseToggle input adapter.

F27 is now frozen because the next attempted direction started mixing Pause, Gate, InputMode, PlayerInput ownership and adapter-module planning. The current state is stable enough to preserve, but not enough to justify more runtime code before the input/adapter matrix is reorganized.

## Accepted current state

```text
F27A — Closed / PASS
Pause UIGlobal surface baseline.

F27B — Closed / PASS
Narrow Unity Input System PauseToggle adapter.

F27C — Closed / Audit PASS
Gate must not become a component blocker.

F27D — Closed / PASS
Pause diagnostics use InputAcceptance / InteractionAcceptance language.
```

## Frozen / cancelled work

| Cut | Name | Status | Decision |
|---|---|---|---|
| F27E | Input Consumers Respect Gate | Cancelled / Do not apply | Direct Gate checks in ordinary input consumers would complicate Pause and hide the missing InputMode boundary. |
| F27F | Pause Freeze Policy Adapter | Deferred | `Time.timeScale` policy should wait until InputMode and PlayerInput ownership are resolved. |
| F27G | Pause Closeout / Usage Guide | Deferred | Closeout should happen after the F28 replan confirms the future boundary. |

## Why F27 is frozen

The current question is larger than Pause:

```text
Who owns the PlayerInput object?
Where do Unity Input System adapters live?
What is the typed InputMode contract?
How does PauseOverlay keep UI input alive while gameplay input stops?
What role remains for Gate after InputMode exists?
Where do player/camera/audio/gameplay adapters belong?
```

Those questions must be answered before further implementation.

## Corrected direction

The preferred shape is now:

```text
PauseRuntime
  -> state/result owner

InputModeRuntime
  -> typed active mode owner

Unity Input System adapter
  -> applies mode to concrete Unity targets

Player/Actor/Input adapter modules
  -> provide concrete PlayerInput targets later

Gate
  -> passive admission / hard-lock / diagnostics, not normal Pause action-map behavior
```

## Gate position after F27D

F27D can remain because it only corrected the diagnostic vocabulary away from broad component/gameplay blocking.

However, F27D must not be read as approval to build this normal path:

```text
Pause -> Gate -> every input consumer checks Gate
```

Gate is not removed. It is demoted from the main Pause/Input solution to a passive/safety/admission layer until F28 clarifies the InputMode boundary.

## Handoff to F28

F28 owns the roadmap correction:

```text
F28 — Roadmap Reconciliation and Adapter Module Spine
```

F28 is documentation-first. It creates the dependency map and adapter/module ownership plan before any runtime service applies action maps.

See:

```text
Assets/_Documentation/Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md
Packages/com.immersive.framework/Documentation~/ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md
Assets/_Documentation/Notes/F27E-CANCELLED-Input-Consumers-Gate-Replan.md
```

## Validation preserved

The frozen F27 state remains validated by the latest smokes:

```text
Pause surface resolved from UIGlobal.
Pause/Resume/Toggle through QA trigger.
PauseToggle through Unity Input System.
Same-frame Player/UI PauseToggle dedupe.
Pause blockers report InputAcceptance / InteractionAcceptance.
PauseRequest remains allowed.
```
