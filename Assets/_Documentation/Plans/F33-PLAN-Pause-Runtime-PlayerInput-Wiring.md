# F33 — Pause Runtime PlayerInput Wiring

Status: Open.

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
