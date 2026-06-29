# F30D — Pause InputMode Request Boundary

## Status

Closed / passive runtime boundary + QA smoke.

## Purpose

F30D connects the logical Pause state to the passive `InputModeRequest` language without creating a framework input manager.

The cut answers one narrow question:

```text
when Pause is Running or Paused, which logical InputMode should be requested?
```

It does not answer how Unity action maps are switched. That remains a later Unity Input adapter concern.

## Accepted Mapping

| Pause state/result | Requested InputMode |
|---|---|
| `Running` | `Gameplay` |
| `Paused` | `PauseOverlay` |

## Runtime Artifact

```text
Packages/com.immersive.framework/Runtime/InputMode/PauseInputModeRequestMapper.cs
```

The mapper is passive. It creates an `InputModeRequest` from a `PauseState` or `PauseResult`.

It does not:

```text
own PlayerInput;
own PlayerInputManager;
switch action maps;
activate/deactivate Unity input;
dispatch Pause requests;
show Pause UI;
change Time.timeScale;
spawn player/actor objects.
```

## QA Evidence

Smoke:

```text
Pause InputMode Request Boundary Smoke
```

Expected steps:

```text
paused-state-to-pause-overlay
running-state-to-gameplay
pause-result-to-pause-overlay
resume-result-to-gameplay
invalid-pause-state-rejected
no-unity-input-behavior
```

All steps must keep:

```text
actionMapSwitching='False'
inputBehavior='False'
playerInputOwnership='none'
playerInputManagerOwnership='none'
```

## Why This Cut Exists

F29 proved explicit Unity Input integration targets.
F30A created passive InputMode request/result language.
F30B corrected the plan so Unity `PlayerInput` and `PlayerInputManager` remain the official input execution components.
F30C validated component evidence.
F30D now lets Pause produce a logical mode request without owning Unity input behavior.

## Next

F30E should close the phase and select the next implementation track. The next track should be a Unity Input adapter plan/proof, not a custom input manager.
