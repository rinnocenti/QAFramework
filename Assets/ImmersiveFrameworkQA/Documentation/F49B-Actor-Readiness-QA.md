# F49B — Actor Readiness QA

Status: QA package delta.

## Objective

Validate the passive Actor readiness contracts introduced by F49B without using FIRSTGAME and without creating a framework runtime lifecycle.

## Scope

- Adds the `Actors` QA module under `Assets/ImmersiveFrameworkQA/Actors/`.
- Adds a synthetic runtime fixture for `ActorReadiness`.
- Adds an editor scene builder for the synthetic QA scene.
- Adds the route to the QA Hub builder.

## Out of scope

- No FIRSTGAME assets.
- No gameplay movement.
- No PlayerEntryCoordinator.
- No PlayerView.
- No ControlBinding.
- No PlayerInputManager bridge.

## How to run

1. Apply the F49A/F49B package delta to `com.immersive.framework` first.
2. Apply this QA delta to `QAFramework`.
3. Let Unity recompile.
4. Run `Immersive Framework QA > Hub > Create or Refresh Hub and Player QA Scenes`.
5. Open `Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity`.
6. Enter Play Mode.
7. Click `Actor Readiness QA`.
8. Confirm logs with prefix `[F49B_ACTOR_READINESS_QA]` and final `status='Succeeded'`.

## Expected smoke coverage

- `ActorReadiness` starts as `NotReady`.
- `MarkReadyForView` produces `ReadyForView`.
- `MarkReadyForControl` produces `ReadyForControl` and implies `ReadyForView`.
- `SetReadiness(false, true)` fails explicitly.
- `MarkFailed` requires an explicit reason.
- `Release` blocks readiness changes.
- `BeginNewCycle` allows a released readiness cycle to restart explicitly.
- `IActorReadiness.CreateSnapshot()` exposes the expected immutable state.

## Expected final log

```text
[F49B_ACTOR_READINESS_QA] status='Succeeded'
```
