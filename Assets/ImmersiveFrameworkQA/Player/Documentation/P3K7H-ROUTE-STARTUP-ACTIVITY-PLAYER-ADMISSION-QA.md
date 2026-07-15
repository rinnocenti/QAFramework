# P3K.7H — Route Startup Activity Player Admission QA

## Purpose

Prove the official Route request integration for a destination Route whose
Startup Activity requires `GameplayReady`.

The smoke calls only:

```text
FrameworkRuntimeHost.RequestRouteAsync
FrameworkRuntimeHost.ClearActivityAsync
```

It does not invoke candidate staging, P3K.7E Begin or P3K.7E Commit manually.

## Setup

Run from a fresh Play Mode session after the normal QA Hub boot.

The smoke creates runtime-only authoring:

```text
P3K.7H Target Activity
  explicit projection: joined player.1
  requirement: GameplayReady

P3K.7H Target Route
  Primary Scene: QA_LifecycleRouteB
  Startup Activity: P3K.7H Target Activity
```

The destination Route uses the existing
`Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteB.unity` scene,
which is already enabled in the QA Build Profile. Its `PrimaryScenePath` is
distinct from `QA_Hub`, so the Route also has a distinct canonical
`RuntimeContentOwner`.

Route runtime ownership is keyed by `PrimaryScenePath`, not by `RouteName`.

## Menu

```text
Immersive Framework
  > QA
    > Player
      > P3K.7H Run Route Startup Activity Player Admission Smoke
```

## Expected

```text
[P3K7H_ROUTE_STARTUP_ACTIVITY_PLAYER_ADMISSION_SMOKE]
status='Passed'
cases='48'
```

## Coverage

```text
waits for provisioning RuntimeReady
real FrameworkRuntimeHost/P3J/P3K authorities
real joined local Player and stable PlayerInput
current Route and Activity roots
current Activity-owned GameplayReady P3J/P3K chain
runtime-authored distinct destination Route
normal FrameworkRuntimeHost Route request
P3K.7E ReadyToCommit before Route transition
previous Activity exit transferred before commit
destination Startup Activity activation-gate commit
destination Route and Activity publication
Route Startup flow and exact Route names in public diagnostics
exact previous/target Activity owners
committed and adopted Slot tokens
previous Actor destruction at Unity frame boundary
previous Activity and Route root cleanup
destination Route root ownership
normal clear of adopted Startup Activity
P3K release before P3J release
destination Activity root cleanup
destination Route root survives Activity clear
stable Session host/Input retention
public contracts contain no Unity object references
```

## One-shot rule

The target Route remains the current runtime Route after the smoke. Runtime-only
Route/Activity authoring therefore remains alive until Play Mode ends. Re-enter
Play Mode before running the smoke again.

## P3K.7H QA FIX1 — canonical Route ownership

The initial smoke incorrectly constructed Route owners as:

```csharp
RuntimeContentOwner.Route(route.RouteName, route.RouteName)
```

The Framework canonical owner is:

```csharp
RuntimeContentOwner.Route(route.PrimaryScenePath, route.RouteName)
```

The first version also copied the current `QA_Hub` scene path into the
destination Route. Two Route assets with the same `PrimaryScenePath` therefore
shared one canonical RuntimeContent owner even when their display names differed.

The corrected smoke uses `QA_LifecycleRouteB` as the destination Primary Scene
and derives both current and target Route owners from `PrimaryScenePath`.
