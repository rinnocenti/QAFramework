# F32F — InputMode Unity PlayerInput Request Application

## Status

Implemented as an experimental QA-facing cut.

## Goal

Compose the full explicit path from a logical `InputModeRequest` to a Unity `PlayerInput` side effect.

This is not an input manager and does not own Unity's `PlayerInputManager`.

## Pipeline

```text
InputModeState + InputModeRequest
  -> InputModeRequestEvaluator
  -> InputModeUnityApplicationPreviewEvaluator
  -> InputModeUnityActionMapPreviewEvaluator
  -> InputModeUnityApplicationPlanEvaluator
  -> InputModeUnityPlayerInputApplication
```

## Canonical behavior

| Request | Expected Unity PlayerInput application |
| --- | --- |
| `Gameplay` | activate `PlayerInput` and select `Player` action map |
| `PauseOverlay` | activate `PlayerInput` and select `UI` action map |
| `FrontendMenu` | activate `PlayerInput` and select `UI` action map |
| `InputLocked` | deactivate `PlayerInput` |
| same current mode | ignore; no side effect |

## Guardrails

F32F still does not:

- call `PlayerInputManager.JoinPlayer`;
- spawn player prefabs;
- move `PlayerActor`;
- read gameplay commands;
- create a custom framework input manager;
- wire itself into `FrameworkRuntimeHost` automatically.

## QA

Use:

```text
Unity Input Diagnostics -> Run InputMode Unity PlayerInput Request Application Smoke
```

Expected steps:

```text
contracts
gameplay-request-applies-player-map
pause-overlay-request-applies-ui-map
inputlocked-request-deactivates-playerinput
already-in-mode-ignored-no-side-effects
missing-playeractor-blocks-before-application
missing-action-map-blocks-before-application
no-playerinputmanager-join-or-spawn
```
