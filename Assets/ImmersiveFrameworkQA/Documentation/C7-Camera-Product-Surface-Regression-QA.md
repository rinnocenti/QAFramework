# C7 — Camera Product Surface Regression QA

Status: Technical QA consolidation  
Scope: `Assets/ImmersiveFrameworkQA`

## Objective

C7 formalizes the current Camera Product Surface QA chain after the C5 QA scene/smoke and the C6 FIRSTGAME proof.

This cut does not introduce a new Camera product contract. It creates a stable regression entry point for the accepted CameraComposer SinglePlayer technical proof.

## Canonical QA Scene

```text
Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera_ProductSurface.unity
```

The legacy Camera QA scene remains diagnostic/compatibility material only. It is not the main Camera Product Surface flow.

## Canonical Smoke

Run:

```text
Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke
```

The C7 runner opens the clean Camera Product Surface scene when safe and delegates to:

```text
Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke
```

The C5 PASS/FAIL log remains the authoritative regression result.

## Expected PASS

```text
[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.
```

The C7 runner should also log:

```text
[QA][C7 Camera Product Surface] C5 smoke invoked. Use the C5 PASS/FAIL log as the authoritative regression result.
```

## What This Proves

```text
CameraComposer Validate passes in the clean QA scene.
First Apply/Rebuild succeeds.
Second Apply/Rebuild succeeds.
Second Apply/Rebuild is idempotent.
Cinemachine rig materializes.
PlayerComposer CameraTarget is used.
PlayerComposer LookAtTarget is used.
Missing PlayerComposer blocks explicitly.
The clean Camera Product Surface scene remains the current QA entry point.
```

## What This Does Not Prove

```text
FIRSTGAME usability.
Route/Activity camera rewrite.
Multiplayer camera.
Split-screen camera.
Runtime camera authority.
Legacy Camera.enabled compatibility flows.
```

## Failure Interpretation

### `target-scene-not-found`

The clean Camera Product Surface QA scene is missing or moved.

### `active-scene-dirty`

The active scene has unsaved changes. Save or discard changes before running the regression runner.

### `c5-smoke-menu-not-found`

The canonical C5 smoke menu is missing or renamed.

### C5 failure log

Treat the C5 failure reason as the authoritative issue. C7 only selects the canonical scene and invokes the canonical C5 smoke.

## Acceptance

C7 is accepted when:

```text
QAFramework compiles.
C7 menu appears.
C7 opens QA_Camera_ProductSurface when safe.
C7 invokes the C5 smoke.
C5 PASS log is produced.
No FIRSTGAME files are modified.
No package files are modified.
```
