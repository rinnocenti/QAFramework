# F32 — InputMode Unity Adapter Application

Status: Open.

## Baseline

F32 starts only after:

```text
F30E closed InputMode as passive request/result language.
F31C closed PlayerActor and Session PlayerInputManager as canonical references.
```

`F31D — PlayerInput Reference Set` is cancelled and is not part of the official sequence.

## Goal

Move from passive `InputModeRequest` semantics to a Unity Input adapter path that can eventually apply modes through official Unity Input System components.

The framework must not become an input manager. It must integrate:

```text
Unity PlayerInput
Unity PlayerInputManager
project-owned InputActionAsset/action maps
framework-owned lifecycle/input mode request language
```

## Sequence

### F32A — InputMode Unity Application Preview

Status: Closed / smoke PASS.

Create a side-effect-free preview evaluator that maps successful logical `InputModeRequestResult` values to required Unity Input evidence:

- `Gameplay` requires `GameplayCommands` target, `PlayerActor` evidence and Session `PlayerInputManager` evidence.
- `PauseOverlay` requires the `GlobalUiPause` target only.
- `FrontendMenu` uses the `GlobalUiPause` target for now.
- `InputLocked` is accepted as no-target-required preview.

No action-map switching or input behavior.

### F32B — InputMode Unity Action Map Preview

Status: Closed / smoke PASS.

Defines project-owned Unity action-map evidence and maps typed `InputMode` values to action-map preview names.

Initial QA bindings:

```text
Gameplay -> Player
PauseOverlay -> UI
FrontendMenu -> UI
InputLocked -> no action map required
```

No action-map switching or input behavior.

### F32C — InputMode Unity Application Plan

Status: Closed / smoke PASS.

Combines the F32A application preview and F32B action-map preview into a dry-run adapter plan:

```text
Gameplay -> SelectActionMap(Player)
PauseOverlay -> SelectActionMap(UI)
FrontendMenu -> SelectActionMap(UI)
InputLocked -> LockInput
```

This is still intent only. No `PlayerInput.SwitchCurrentActionMap`, `PlayerInput.ActivateInput`, `PlayerInput.DeactivateInput`, `PlayerInputManager.JoinPlayer`, actor spawn or movement is allowed.

### F32D — InputMode Unity PlayerInput Adapter

Status: Implemented / awaiting smoke.

Adds the first explicit Unity PlayerInput side effect:

```text
SelectActionMap -> PlayerInput.SwitchCurrentActionMap(actionMapName)
LockInput -> PlayerInput.DeactivateInput()
```

This is adapter-owned and explicit. It does not own `PlayerInputManager`, call join, spawn actors, move PlayerActor objects or create a custom framework input manager.

## Guardrails

- `PlayerInput` remains an official Unity component.
- `PlayerInputManager` remains Session-scoped evidence and Unity authority.
- `PlayerActor` is the framework-recognized player entity, but movement/spawn stays out.
- Framework core owns typed `InputMode` language, not Unity action-map strings.
- Pause may request modes but does not own PlayerInput.
