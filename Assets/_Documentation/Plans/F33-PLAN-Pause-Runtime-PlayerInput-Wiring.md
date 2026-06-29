# F33 — Pause Runtime PlayerInput Wiring

Status: Open. F33A completed pending closeout; F33B adds the opt-in InputAction trigger.

## Purpose

F33 starts after F32H. F32 proved the explicit `InputMode` / `PauseResult` to Unity `PlayerInput` application path, but deliberately kept it out of `PauseRuntime` and `FrameworkRuntimeHost`.

F33 introduces opt-in runtime wiring that lets an authored scene component submit logical Pause requests and apply the resulting InputMode to an explicit Unity `PlayerInput`.

## Boundary

Accepted:

```text
scene-authored opt-in bridge;
PauseRuntime request submission;
safe preflight before mutating Pause state;
PlayerInput ActivateInput / SwitchCurrentActionMap / DeactivateInput through the existing F32 lane;
no automatic FrameworkRuntimeHost registration.
```

Rejected:

```text
framework-owned input manager;
PlayerInputManager.JoinPlayer;
player prefab spawn;
PlayerActor movement;
gameplay command reading;
hidden PauseRuntime side effects;
automatic FrameworkRuntimeHost wiring.
```

## First cut

F33A — Pause Runtime PlayerInput Bridge.

It creates `PauseInputModeUnityPlayerInputRuntimeBridge`, an opt-in component with public Pause/Resume/Toggle methods. The bridge checks PlayerInput, PlayerActor, UnityInputTarget and Session PlayerInputManager evidence before submitting a Pause request, so missing evidence/action maps cannot put Pause state and Unity input state out of sync.


## Second cut

F33B — Pause InputAction Runtime Bridge Trigger.

It creates `PauseInputActionRuntimeBridgeTrigger`, an opt-in scene component that validates a configured Unity Input System action, normally `UI/Pause`, and forwards it to the F33A bridge. The trigger does not switch action maps itself, does not own `PlayerInputManager`, does not call `JoinPlayer`, and does not spawn player prefabs.
