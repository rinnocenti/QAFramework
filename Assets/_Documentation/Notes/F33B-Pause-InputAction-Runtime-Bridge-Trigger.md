# F33B — Pause InputAction Runtime Bridge Trigger

Status: pending smoke.

## What changed

F33B adds an opt-in Unity `InputAction` trigger for the F33A Pause runtime PlayerInput bridge:

```text
PauseInputActionRuntimeBridgeTrigger
```

This component connects a concrete Unity Input System action, such as `UI/Pause`, to the already explicit `PauseInputModeUnityPlayerInputRuntimeBridge`.

## Runtime path

```text
Unity InputAction performed
  -> PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> FrameworkRuntimeHost Pause request
  -> PauseResult
  -> InputMode
  -> Unity PlayerInput application
```

## Boundary

Accepted:

```text
scene-authored opt-in trigger;
InputAction evidence validation;
explicit bridge submission;
Pause/Resume/Toggle request kind;
no automatic FrameworkRuntimeHost wiring;
no PlayerInputManager ownership.
```

Rejected:

```text
framework-owned input manager;
PlayerInputManager.JoinPlayer;
player prefab spawn;
PlayerActor movement;
gameplay command reading;
direct action-map switching from the trigger.
```

The trigger itself does not call `SwitchCurrentActionMap`, `ActivateInput`, `DeactivateInput`, `JoinPlayer` or spawn. It only submits to the F33A bridge after validating the configured action evidence.

## QA

Run:

```text
Run Pause InputAction Runtime Bridge Trigger Smoke
```

Expected steps:

```text
contracts
pause-action-evidence-valid-no-side-effect
pause-action-toggle-applies-ui-map
pause-action-toggle-resumes-player-map
missing-pause-action-blocks-before-bridge
missing-bridge-blocking
no-playerinputmanager-join-or-spawn
```
