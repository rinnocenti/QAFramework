# F33D — Pause Input Diagnostics Flattening

Status: Implemented.

## Purpose

F33B/F33C validated the canonical Pause input path, but the QA diagnostics became too noisy because trigger results embedded the full bridge diagnostic string, and bridge results embedded the full PlayerInput application diagnostic string.

This cut keeps the same runtime behavior and flattens diagnostic output.

## Accepted

```text
PauseInputActionRuntimeBridgeTriggerResult exposes concise trigger fields.
If a bridge result exists, diagnostics include bridgeStatus and bridgePauseStatus only.
PauseInputModeUnityPlayerInputRuntimeBridge stores a concise application message instead of the full nested application diagnostic string.
No runtime behavior changes.
```

## Rejected

```text
No change to Pause runtime.
No change to InputMode mapping.
No change to PlayerInput application.
No PlayerInputManager.JoinPlayer.
No actor spawning.
No custom input manager.
```

## Validation

Run:

```text
Run Pause InputAction Runtime Bridge Trigger Smoke
```

Expected:

```text
all F33B smoke steps still pass;
diagnostics remain readable;
no nested bridge/application diagnostic blob appears inside the diagnostics field.
```
