# F32 — InputMode Unity Adapter Application

Status: Closed through F32H.

## Baseline

F32 starts only after:

```text
F30E closed InputMode as passive request/result language.
F31C closed PlayerActor and Session PlayerInputManager as canonical references.
```

`F31D — PlayerInput Reference Set` is cancelled and is not part of the official sequence.

## Goal

Move from passive `InputModeRequest` semantics to an explicit Unity `PlayerInput` adapter path that applies modes through official Unity Input System components without turning the framework into an input manager.

The framework integrates:

```text
Unity PlayerInput
Unity PlayerInputManager as Session-scoped evidence
project-owned InputActionAsset/action maps
framework-owned lifecycle/input mode request language
```

## Closed sequence

### F32A — InputMode Unity Application Preview

Status: Closed / smoke PASS.

Side-effect-free preview of whether a successful `InputModeRequestResult` has enough Unity Input evidence for later application.

Rules:

- `Gameplay` requires `GameplayCommands`, `PlayerActor` evidence and Session `PlayerInputManager` evidence.
- `PauseOverlay` requires `GlobalUiPause`.
- `FrontendMenu` uses `GlobalUiPause` for now.
- `InputLocked` requires no target.

### F32B — InputMode Unity Action Map Preview

Status: Closed / smoke PASS.

Defines initial project action-map evidence:

```text
Gameplay -> Player
PauseOverlay -> UI
FrontendMenu -> UI
InputLocked -> no action map required
```

No action-map switching or input behavior.

### F32C — InputMode Unity Application Plan

Status: Closed / smoke PASS.

Combines F32A and F32B into a dry-run application plan:

```text
Gameplay -> SelectActionMap(Player)
PauseOverlay -> SelectActionMap(UI)
FrontendMenu -> SelectActionMap(UI)
InputLocked -> LockInput
```

No Unity side effect.

### F32D — InputMode Unity PlayerInput Adapter

Status: Closed / smoke PASS.

First explicit Unity `PlayerInput` side effect:

```text
SelectActionMap -> PlayerInput.SwitchCurrentActionMap(actionMapName)
LockInput -> PlayerInput.DeactivateInput()
```

No `PlayerInputManager.JoinPlayer`, spawn, movement or custom input manager.

### F32E — InputMode Unity PlayerInput Application

Status: Closed / smoke PASS.

Adds activation semantics over F32D:

```text
SelectActionMap -> PlayerInput.ActivateInput() then SwitchCurrentActionMap(actionMapName)
LockInput -> PlayerInput.DeactivateInput()
```

This handles the unlock path after `InputLocked`.

### F32F — InputMode Unity PlayerInput Request Application

Status: Closed / smoke PASS.

Composes the full explicit request-to-`PlayerInput` path:

```text
InputModeState + InputModeRequest
  -> InputModeRequestEvaluator
  -> Unity evidence preview
  -> action-map preview
  -> application plan
  -> PlayerInput application
```

Already-in-mode requests are ignored without side effects. Missing evidence/action-map cases fail before Unity input mutation.

### F32G — Pause InputMode Unity PlayerInput Application

Status: Closed / smoke PASS.

Bridges completed logical `PauseResult` values to explicit `InputMode` request application against one Unity `PlayerInput`.

```text
Paused  -> PauseOverlay -> UI
Running -> Gameplay -> Player
```

No automatic `PauseRuntime` or `FrameworkRuntimeHost` wiring.

### F32H — Closeout

Status: Closed.

Closes F32 and selects a later runtime wiring phase.

Reference: `Assets/_Documentation/Notes/F32H-InputMode-Unity-PlayerInput-Application-Closeout.md`.

## Guardrails preserved

- `PlayerInput` remains the official Unity component being adapted.
- `PlayerInputManager` remains Session-scoped evidence and Unity authority.
- The framework does not create a custom input manager.
- F32 does not call `PlayerInputManager.JoinPlayer`.
- F32 does not spawn player prefabs.
- F32 does not move `PlayerActor` objects.
- F32 does not read gameplay commands.
- F32 does not wire itself automatically into `PauseRuntime` or `FrameworkRuntimeHost`.
