# F32G — Pause InputMode Unity PlayerInput Application

## Status

Implemented as an experimental QA-facing cut.

## Goal

Bridge a completed logical `PauseResult` into the explicit `InputMode -> Unity PlayerInput` application chain.

```text
PauseResult
  -> PauseInputModeRequestMapper
  -> InputModeUnityPlayerInputRequestApplication
  -> PlayerInput.ActivateInput / SwitchCurrentActionMap / DeactivateInput
```

## Canonical behavior

| Pause result | InputMode request | Unity PlayerInput application |
| --- | --- | --- |
| `Applied` to `Paused` | `PauseOverlay` | activate `PlayerInput` and select `UI` action map |
| `Applied` to `Running` | `Gameplay` | activate `PlayerInput` and select `Player` action map |
| `IgnoredNoChange` with matching InputMode | ignored | no side effect |
| `Rejected` / `Failed` | blocked | no side effect |

## Guardrails

F32G still does not:

- wire itself automatically into `PauseRuntime`;
- wire itself automatically into `FrameworkRuntimeHost`;
- call `PlayerInputManager.JoinPlayer`;
- spawn player prefabs;
- move `PlayerActor`;
- read gameplay commands;
- create a custom framework input manager.

## QA

Use:

```text
Unity Input Diagnostics -> Run Pause InputMode Unity PlayerInput Application Smoke
```

Expected steps:

```text
contracts
pause-result-applies-ui-map
resume-result-applies-player-map
ignored-pause-result-current-mode-no-side-effects
rejected-pause-result-blocks-before-application
missing-playeractor-blocks-before-application
no-playerinputmanager-join-or-spawn
```
