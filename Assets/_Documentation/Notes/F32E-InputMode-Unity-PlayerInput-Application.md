# F32E — InputMode Unity PlayerInput Application

Status: Implemented / awaiting smoke.

## Purpose

F32D proved the explicit `PlayerInput` adapter can select action maps and lock input. F32E adds the missing application-level semantic:

```text
leaving InputLocked must reactivate PlayerInput before selecting Gameplay/UI maps
```

## Runtime shape

New runtime entry:

```text
InputModeUnityPlayerInputApplication.Apply(plan, playerInput, source, reason)
```

It is explicit and local. It is not a manager.

## Behavior

```text
SelectActionMap -> PlayerInput.ActivateInput() + adapter SwitchCurrentActionMap(...)
LockInput -> adapter PlayerInput.DeactivateInput()
```

The application wrapper pre-validates the requested action map before activation. Missing maps fail before side effects.

## Non-goals

F32E does not:

```text
own PlayerInputManager
call PlayerInputManager.JoinPlayer
spawn PlayerActor/player prefab
read gameplay commands
move the player
integrate real Pause dispatch
create a framework input manager
```

## QA

Button:

```text
Run InputMode Unity PlayerInput Application Smoke
```

Expected steps:

```text
contracts
gameplay-activates-and-selects-player-map
pause-overlay-activates-and-selects-ui-map
inputlocked-deactivates-playerinput
locked-to-gameplay-reactivates-player-map
missing-action-map-blocking-before-activation
failed-plan-blocking
no-playerinputmanager-join-or-spawn
```
