# F28-ADR-INPUT-001 — InputMode and Adapter Boundary

Status: Proposed / planning gate
Phase: F28 — InputMode and Adapter Boundary Reorganization
Type: Framework Core / Unity Adapter Boundary / Input

## Context

F27 validated Pause UIGlobal surface and PauseToggle input. During the next attempted step, the design started pushing Gate checks into individual input consumers.

That direction was rejected because the framework has not yet settled PlayerInput ownership, InputMode semantics or adapter boundaries.

## Decision

InputMode must be planned before additional Pause/Input runtime wiring.

The canonical shape is:

```text
PauseRuntime
  state/result owner

InputMode contract/runtime
  typed mode owner

Unity Input System adapter
  applies mode to concrete Unity input targets

Player/Actor/Input adapter modules
  provide targets and gameplay consumption later

Gate
  passive admission/hard-lock/safety language
```

## Accepted Rules

- `InputMode` is a typed contract, not an action-map string.
- Unity action-map names are adapter configuration, not framework identity.
- Pause may request an InputMode change, but Pause must not own PlayerInput.
- InputMode may apply Unity Input System effects, but it must not own Route/Activity lifecycle.
- Gate must not become a component blocker.
- Gate must not become a mandatory check in every normal input consumer just to make Pause work.
- Adapter modules consume framework contracts; they do not redefine Pause, Gate, Route or Activity ownership.

## Rejected Direction

```text
Pause -> Gate -> every input consumer checks Gate
```

This scatters Pause behavior across consumers and hides the missing InputMode boundary.

## Preferred Pause Direction

```text
Running:
  InputMode = Gameplay
  Gameplay input is available
  UI may be available by policy

Paused:
  InputMode = PauseOverlay
  UI input remains available
  PauseToggle / Cancel remains available
  gameplay command maps do not drive gameplay
```

`Time.timeScale` remains a later freeze policy and does not replace InputMode.

## NewScripts Reference

The old project shows a useful separation:

```text
SessionActivityPauseToggleInputAdapter
  narrow PauseToggle input signal

InputModeService
  typed mode requests and action-map application

ADR-0007
  Gate/InputMode execute effects; they do not decide lifecycle
```

The framework should bring the boundary, not copy the old service shape directly.

## Open Questions Before Runtime

F28A must answer:

```text
who supplies PlayerInput targets
whether PlayerInput belongs to project, player adapter module or framework surface
whether InputMode is core-only state first or Unity-applied immediately
how UI input stays active during PauseOverlay
how multiple players/slots are represented later
what diagnostics prove mode application without gameplay dependencies
```

## Consequences

F27E input-consumer Gate wiring is cancelled.

F28 begins with audit and plan correction. Runtime work resumes only after PlayerInput ownership and adapter placement are explicit.
