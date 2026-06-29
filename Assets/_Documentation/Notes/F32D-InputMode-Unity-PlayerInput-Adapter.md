# F32D — InputMode Unity PlayerInput Adapter

Status: Implemented / awaiting smoke.

## Purpose

F32D introduces the first explicit Unity Input side effect in the InputMode lane.

Previous F32 cuts were passive:

```text
F32A -> validate Unity Input evidence
F32B -> validate action-map evidence
F32C -> build a dry-run application plan
```

F32D adds an explicit adapter that can apply a successful F32C plan to a concrete Unity `PlayerInput`.

## Decision

The framework still does not become an input manager.

The adapter may call Unity `PlayerInput` methods only when it receives:

```text
a successful InputModeUnityApplicationPlanResult
an explicit PlayerInput instance
a valid action map on PlayerInput.actions, when SelectActionMap is requested
```

## Supported operations

```text
SelectActionMap -> PlayerInput.SwitchCurrentActionMap(actionMapName)
LockInput -> PlayerInput.DeactivateInput()
```

## Not supported here

F32D does not:

```text
own PlayerInputManager
call PlayerInputManager.JoinPlayer
spawn player prefabs
move PlayerActor
create a framework input manager
bind input actions
read gameplay commands
own Pause runtime
```

## QA smoke

Button:

```text
Run InputMode Unity PlayerInput Adapter Smoke
```

Expected steps:

```text
contracts
gameplay-selects-player-action-map
pause-overlay-selects-ui-action-map
inputlocked-deactivates-playerinput
missing-action-map-blocking
failed-plan-blocking
no-playerinputmanager-join-or-spawn
```

## Notes

This is intentionally the first place where `actionMapSwitching='True'` can appear in QA output. That value is now acceptable only inside the explicit PlayerInput adapter smoke, not inside preview/plan smokes.
