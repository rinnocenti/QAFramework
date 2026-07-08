# F49C — Actor Readiness Behaviour QA

This QA validates the Unity-facing `ActorReadinessBehaviour` adapter added by F49C.

## Purpose

Prove that a scene-authored GameObject can expose `IActorReadiness` without creating PlayerEntry, PlayerView, ControlBinding, PlayerInputManager integration or gameplay movement.

## Build / Refresh

Run:

```text
Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes
```

Or directly:

```text
Immersive Framework QA > Actors > Create or Refresh F49C Actor Readiness Behaviour QA Scene
```

## Smoke path

```text
1. Open Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity.
2. Enter Play Mode.
3. Click Actor Readiness Behaviour QA.
4. Check the Console for [F49C_ACTOR_READINESS_BEHAVIOUR_QA].
```

## Expected final log

```text
[F49C_ACTOR_READINESS_BEHAVIOUR_QA] status='Succeeded'
```

## Coverage

```text
ActorReadinessBehaviour component exists.
ActorReadinessBehaviour implements IActorReadiness.
ReadyForView snapshot works through the component.
ReadyForControl snapshot works through the component.
SetReadiness(false, true) fails explicitly.
Failed requires explicit reason.
Release blocks readiness changes.
BeginNewCycle reopens the component readiness cycle.
```

## Boundary

This QA is synthetic and must remain under `Assets/ImmersiveFrameworkQA/`. FIRSTGAME integration is not part of F49C.
