# F33A — Pause Runtime PlayerInput Bridge

Status: pending smoke.

## What changed

F33A adds the first opt-in runtime bridge from logical Pause requests to Unity `PlayerInput` application:

```text
PauseInputModeUnityPlayerInputRuntimeBridge
```

The bridge is scene-authored. It is not registered automatically in `FrameworkRuntimeHost` and it does not mutate `PauseRuntime` unless its preflight succeeds.

## Runtime path

```text
Pause/Resume/Toggle call on bridge
  -> read current Pause snapshot
  -> preflight future InputMode / evidence / action-map / plan
  -> submit PauseRequest to FrameworkRuntimeHost
  -> apply PauseResult through F32G PlayerInput path
```

## Safety rule

The bridge checks evidence/action-map viability before submitting the Pause request. If the future InputMode cannot be applied, it returns `FailedPreflight` and does not submit the Pause request.

This protects against:

```text
Pause state changes but PlayerInput cannot switch to UI;
Resume state changes but PlayerInput cannot switch to Player;
missing PlayerActor evidence for Gameplay;
missing Session PlayerInputManager evidence;
missing action map evidence.
```

## Accepted Unity side effects

Only through the explicit F32 PlayerInput path:

```text
ActivateInput();
SwitchCurrentActionMap("UI" / "Player");
DeactivateInput();
```

## Still out of scope

```text
automatic FrameworkRuntimeHost wiring;
automatic PauseRuntime observer/event;
PlayerInputManager.JoinPlayer;
player prefab spawn;
PlayerActor movement;
gameplay command reading;
framework-owned input manager.
```

## QA

Run:

```text
Run Pause Runtime PlayerInput Bridge Smoke
```

Expected steps:

```text
contracts
pause-request-applies-ui-map
resume-request-applies-player-map
already-paused-request-ignored-no-side-effects
missing-playeractor-preflight-blocks-before-pause-request
missing-action-map-preflight-blocks-before-pause-request
no-playerinputmanager-join-or-spawn
```
