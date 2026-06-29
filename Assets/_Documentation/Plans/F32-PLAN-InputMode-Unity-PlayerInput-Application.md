# F32 — InputMode Unity PlayerInput Application Plan

Status: Closed through F32H.

## Purpose

This plan records the concrete `PlayerInput` application lane that follows the side-effect-free F32A/F32B/F32C previews.

## F32D — InputMode Unity PlayerInput Adapter

Status: closed / smoke PASS.

Adds explicit, local `PlayerInput` adapter calls:

```text
SelectActionMap -> SwitchCurrentActionMap(actionMapName)
LockInput -> DeactivateInput()
```

## F32E — InputMode Unity PlayerInput Application

Status: closed / smoke PASS.

Wraps the adapter and applies activation semantics:

```text
SelectActionMap -> ActivateInput() then SwitchCurrentActionMap(actionMapName)
LockInput -> DeactivateInput()
```

## F32F — InputMode Unity PlayerInput Request Application

Status: closed / smoke PASS.

Composes a full `InputModeRequest` into explicit `PlayerInput` application.

## F32G — Pause InputMode Unity PlayerInput Application

Status: closed / smoke PASS.

Bridges completed logical `PauseResult` values to explicit `PlayerInput` application.

This is QA-facing and not automatically wired into `PauseRuntime` or `FrameworkRuntimeHost`.

## F32H — Closeout

Status: closed.

F32 is complete as an explicit application path. Runtime host wiring is out of scope and must be handled as a later phase.

## Out of scope for all F32 cuts

- framework-owned input manager;
- `PlayerInputManager.JoinPlayer`;
- player prefab spawn;
- player movement;
- gameplay command reading;
- automatic `PauseRuntime` wiring;
- automatic `FrameworkRuntimeHost` wiring.
