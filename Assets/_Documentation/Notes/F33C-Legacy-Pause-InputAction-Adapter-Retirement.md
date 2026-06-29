# F33C — Legacy Pause InputAction Adapter Retirement

Status: Implemented.

## Reason

F33B establishes the canonical authored Pause input path:

```text
Unity InputAction
  -> PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> FrameworkRuntimeHost Pause request
  -> PauseResult
  -> InputMode
  -> Unity PlayerInput application
```

The older F27B `UnityPauseInputActionAdapter` submitted a logical Pause request directly. That was acceptable before `InputMode` and `PlayerInput` application existed, but it is now incomplete because it can change Pause state without selecting the matching Unity action map.

## Decision

`UnityPauseInputActionAdapter` is retired as an active runtime path. The class is retained only as an inert migration stub so existing scenes do not become missing-script objects immediately.

The stub now:

```text
logs that the legacy adapter is removed;
does not subscribe to InputAction callbacks;
does not submit Pause requests;
does not switch action maps;
does not activate/deactivate PlayerInput;
does not call PlayerInputManager.JoinPlayer;
does not spawn actors.
```

## Canonical replacement

Use:

```text
PauseInputActionRuntimeBridgeTrigger
PauseInputModeUnityPlayerInputRuntimeBridge
```

The trigger validates the authored action, normally `UI/Pause`, and forwards the request to the bridge. The bridge performs preflight before mutating Pause state and applies the resulting `InputMode` to the explicit `PlayerInput`.

## Non-goals

F33C does not add automatic `FrameworkRuntimeHost` wiring, does not own `PlayerInputManager`, does not call `JoinPlayer`, does not spawn players and does not read gameplay commands.

## Validation

F33B smoke remains the functional proof for the canonical path. F33C is a retirement/redirect cut: compile plus the existing F33B smoke are sufficient.
