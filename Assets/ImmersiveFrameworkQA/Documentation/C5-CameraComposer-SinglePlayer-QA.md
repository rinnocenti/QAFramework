# C5 — CameraComposer SinglePlayer QA

Status: QAFramework smoke delta
Surface: Camera Recipe / Camera Composer
Package dependency: `com.immersive.framework`

## Purpose

Provide an isolated QAFramework smoke for the C5 CameraComposer SinglePlayer MVP inside the canonical Camera QA scene.

This smoke replaces ad-hoc manual scene setup for the product-surface validation step.

## Menu

```text
Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke
```

## What it builds

The smoke opens `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity` and creates an isolated root there:

```text
QA_C5_CameraComposer_SinglePlayer_Smoke
  QA_PlayerPrototype
    CameraTarget
    LookAtTarget
  QA_CameraRig
  QA_Negative_MissingPlayerComposer
```

The QA player receives a package `PlayerComposer` with explicit `CameraTarget` and `LookAtTarget` references.

The QA camera rig receives a package `CameraComposer` configured as:

```text
mode = SinglePlayerFollowCamera
ownershipScope = SinglePlayer
targetSourceKind = PlayerComposer
followRequirement = Required
lookAtRequirement = Optional
priority = 10
```

## What it validates

```text
Validate succeeds on the configured CameraComposer.
First Apply/Rebuild succeeds and materializes the Cinemachine rig.
Second Apply/Rebuild succeeds with created='0'.
Second Apply/Rebuild reports already-valid materialization.
Missing PlayerComposer blocks explicitly.
```

## Expected PASS log

```text
[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.
```

## Boundary

This smoke does not validate FIRSTGAME usability. FIRSTGAME proof remains a later consumer-validation cut.

The former `PlayerView Camera Activation QA` flow remains available through its route and scene for compatibility regression, but it is no longer a primary Hub entry because it validates the legacy camera-activation contract rather than the Cinemachine-first Camera Product Surface.

This smoke does not validate multiplayer, RouteCamera, ActivityCamera, PlayerSlot resolution, shared group cameras or spectator/debug cameras.
