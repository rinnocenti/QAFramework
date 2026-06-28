# IF-FW-F27B — Pause Input Binding

## Status

Closed / pending smoke

## Purpose

Connect authored Unity Input System `PauseToggle` actions to the canonical framework Pause request path.

This cut keeps Pause input intentionally narrow:

```text
Unity Input System action performed
→ UnityPauseInputActionAdapter
→ FrameworkRuntimeHost.RequestPause(Toggle)
→ PauseRuntime
→ PauseSurfaceRuntime
```

## What changed

- Added `UnityPauseInputActionAdapter` under `Packages/com.immersive.framework/Runtime/Pause`.
- Added `Unity.InputSystem` as a runtime asmdef reference.
- Added `PauseToggle` actions to the project `InputSystem_Actions.inputactions` asset in both `Player` and `UI` maps.
- Added default bindings:
  - `<Keyboard>/escape`
  - `<Gamepad>/start`
- Added the adapter to `QA_UIGlobal` using the existing `InputSystem_Actions` asset.

## Boundary

This cut does **not** implement InputMode ownership.

The adapter does not:

- switch action maps;
- own `PlayerInput`;
- lock gameplay input;
- change `Time.timeScale`;
- own Pause UI;
- infer Gameplay/UI/PauseOverlay modes.

Those remain later cuts.

## Behavior preserved from NewScripts reference

The old `SessionActivityPauseToggleInputAdapter` had two important behaviors worth preserving:

1. Listen to `PauseToggle` from both `Player` and `UI` maps.
2. Dedupe same-frame events, because the same physical input can be observed by more than one enabled action.

F27B preserves those behaviors in framework shape.

## Acceptance

On boot, expected log:

```text
Pause Input Action Adapter ready. asset='InputSystem_Actions' playerAction='Player/PauseToggle' uiAction='UI/PauseToggle'
```

Pressing Escape / Start should produce:

```text
Pause Input Action performed. action='Player/PauseToggle' ... currentState='Paused'
```

or:

```text
Pause Input Action performed. action='UI/PauseToggle' ... currentState='Running'
```

The existing Pause request log should still appear:

```text
Pause Request completed. kind='Toggle' source='UnityPauseInputActionAdapter' currentState='Paused|Running' pauseSurface='Succeeded'
```

## Notes

The input signal is a command. It should remain separate from InputMode policy.

A later InputMode cut can decide how action maps are selected, but it should not replace this narrow PauseToggle adapter unless there is a stronger canonical input boundary.
