# F31C — PlayerActor / Session Unity Input Reference Closeout

## Status

Closed / documentation closeout.

## Purpose

F31 closes the canonical reference layer needed before real InputMode application.

F30 created passive input mode requests. F31 identifies the minimum objects that those requests may target later:

```text
PlayerActor : IActor, with required Unity PlayerInput evidence;
Session-scoped Unity PlayerInputManager integration evidence.
```

## Closed cuts

| Cut | Status | Output |
|---|---|---|
| F31A — PlayerActor Identity and PlayerInput Evidence | Closed / QA PASS | Adds minimal `IActor`, `ActorId`, `ActorKind`, `PlayerActorDeclaration` and PlayerInput evidence validation. |
| F31B — Session PlayerInputManager Boundary | Closed / QA PASS | Declares and validates Session-scoped Unity `PlayerInputManager` evidence. Rejects Activity-scoped manager ownership. |
| F31B1 — Session PlayerInputManager Smoke Warning Fix | Closed / compile warning fix | Removes the redundant same-variable comparison from the smoke contract check. |
| F31C — PlayerActor / Session Unity Input Reference Closeout | Closed | Records the canonical reference model and selects the next input adapter direction. |

## Accepted reference model

| Reference | Scope | Owner | Rule |
|---|---|---|---|
| `IActor` | Framework identity | Framework | Minimal actor contract only. |
| `PlayerActorDeclaration` | Unity-facing actor evidence | Framework adapter surface | Represents a player actor recognized by the framework. |
| `PlayerInput` on PlayerActor | Unity input execution evidence | Unity official component / project configuration | Required for `PlayerActor`. Framework validates presence but does not own behavior. |
| `SessionPlayerInputManagerDeclaration` | Session-level Unity Input evidence | Framework adapter surface | Declares the one Session integration point for Unity `PlayerInputManager`. |
| `PlayerInputManager` | Session | Unity official component / project configuration | Must be validated as singleton evidence when required. Not Activity-owned. |
| InputActionAsset/action maps | Project | Project configuration | Not authored in framework core. |
| Movement/gameplay commands | Project/gameplay adapter | Future module | Not part of F31. |

## Why Session scope is required

`PlayerInputManager` must be available before a gameplay Activity exists.

Example:

```text
Route: Character Creation
```

A route like this may need player presence, device pairing, UI navigation or character selection before the gameplay Activity is active. Therefore, the canonical `PlayerInputManager` integration point belongs to Session scope.

Activities may consume player actors. They do not own the canonical manager.

Routes may require player presence. They do not own the canonical manager.

## Final F31 artifact set

Actor identity:

```text
Runtime/Actors/ActorId.cs
Runtime/Actors/ActorKind.cs
Runtime/Actors/IActor.cs
Runtime/Actors/PlayerActorDeclaration.cs
Runtime/Actors/PlayerActorDescriptor.cs
Runtime/Actors/PlayerActorSet.cs
Runtime/Actors/PlayerActorSetIssue.cs
Runtime/Actors/PlayerActorSetIssueKind.cs
Runtime/Actors/PlayerActorValidator.cs
```

Session Unity Input manager evidence:

```text
Runtime/UnityInput/UnityInputPlayerInputManagerScope.cs
Runtime/UnityInput/SessionPlayerInputManagerDeclaration.cs
Runtime/UnityInput/UnityInputPlayerInputManagerEvidence.cs
Runtime/UnityInput/UnityInputTargetValidator.cs
```

QA smokes:

```text
PlayerActor Identity Smoke
Session PlayerInputManager Boundary Smoke
```

## Explicit non-goals

F31 does not:

```text
spawn a player actor;
call PlayerInputManager.JoinPlayer;
instantiate a player prefab;
switch action maps;
activate or deactivate PlayerInput;
read input actions;
move the player;
bind camera/audio/save behavior;
create a custom input manager;
attach Pause to concrete Unity input behavior.
```

## Closeout criteria result

| Criterion | Result |
|---|---|
| Minimal player actor identity exists | Met. |
| PlayerActor requires PlayerInput evidence | Met. |
| Duplicate PlayerActor ids are blocking | Met. |
| Session PlayerInputManager evidence exists | Met. |
| Missing required Session manager is blocking | Met. |
| Duplicate Session managers are blocking | Met. |
| Manager is Session-scoped, not Activity-scoped | Met. |
| No Unity input behavior is executed | Met. |

## Selected next direction

The next input cut may return to InputMode application, but only as an explicit Unity adapter surface.

The next accepted direction is:

```text
InputMode request/result
  -> validate canonical target evidence
  -> plan Unity PlayerInput action-map application
  -> do not execute switching until the adapter cut explicitly says so
```

The next phase must not revive the rejected framework-owned input manager design.
