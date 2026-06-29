# F32C — InputMode Unity Application Plan

Status: Implemented / awaiting smoke.

## Decision

F32C creates a side-effect-free Unity Input application plan. It combines:

```text
F32A InputMode Unity application preview
F32B InputMode Unity action-map preview
```

The output is an immutable dry-run result that says what the Unity Input adapter would do later.

## Operations

```text
Gameplay      -> SelectActionMap(Player)
PauseOverlay  -> SelectActionMap(UI)
FrontendMenu  -> SelectActionMap(UI)
InputLocked   -> LockInput
```

These are plan operations only. F32C does not call Unity Input behavior.

## Explicit non-goals

F32C does not:

```text
call PlayerInput.SwitchCurrentActionMap
activate or deactivate PlayerInput
call PlayerInputManager.JoinPlayer
spawn a player prefab
move PlayerActor
create a framework-owned input manager
```

## QA

Run:

```text
Run InputMode Unity Application Plan Smoke
```

Expected steps:

```text
contracts
gameplay-builds-player-action-map-plan
pause-overlay-builds-ui-action-map-plan
frontend-menu-builds-ui-action-map-plan
inputlocked-builds-lock-plan
failed-action-map-preview-blocking
preview-mode-mismatch-blocking
no-unity-input-behavior
```

All side-effect flags must remain false:

```text
actionMapSwitching='False'
inputBehavior='False'
playerInputActivation='False'
playerJoin='False'
actorSpawning='False'
```
