# F27E Cancelled — Input Consumers Gate Replan

## Status

Cancelled / Do not apply runtime patch

## Decision

`IF-FW-F27E-input-consumers-respect-gate-patch.zip` must not be applied.

The proposed runtime direction made individual input consumers consult Gate directly before accepting commands. That proved the Gate admission model, but it also pushed Pause/Input toward a reactive and over-distributed design.

## Reason

The current problem is not that every gameplay input consumer needs a Gate query.

The current problem is that the framework has not yet decided:

```text
who owns the active PlayerInput target
which typed InputMode is active
which action maps are active in each mode
how UI input remains available during Pause
where Unity Input System adapters belong
```

Without those decisions, wiring every consumer to Gate would hide a missing InputMode boundary and force future gameplay adapters to duplicate admission logic.

## Accepted Current State

F27 is frozen at:

```text
F27A — Closed / PASS
Pause UIGlobal surface baseline.

F27B — Closed / PASS
Narrow PauseToggle input adapter.

F27C — Closed / Audit PASS
Gate must not become a component blocker.

F27D — Closed / PASS
Pause diagnostics reframed to InputAcceptance / InteractionAcceptance, but this does not make Gate the primary Pause/Input implementation path.
```

## Rejected Direction

Do not implement this as the normal Pause path:

```text
Pause
  -> Gate blocker
    -> every Move / Interact / Fire / Look consumer checks Gate
```

This creates scattered behavior and makes Pause harder to reason about.

## Preferred Direction

Pause should drive a typed InputMode after the InputMode boundary is planned:

```text
PauseRuntime
  -> InputModeRuntime requests PauseOverlay
    -> Unity Input adapter applies UI/Pause map policy
      -> gameplay action maps stop driving gameplay
      -> UI and PauseToggle remain available
```

Gate remains available for lifecycle admission, hard locks, diagnostics and exceptional command gating, but it is not the primary mechanism for ordinary Pause action-map behavior.

## Next Planning Step

Before more runtime implementation, audit and reorganize the roadmap around:

```text
InputMode contract
PlayerInput ownership
Unity Input System adapter boundary
Pause <-> InputMode relationship
Gate's reduced/passive role
Adapter module ownership
```

The next phase should be planning/audit-first, not runtime-first.
