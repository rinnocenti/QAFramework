# POST-RESET-H1C — QA Camera Route/Activity Cut

This cut corrects the target of the first camera workstream: camera validation starts in QA, not FIRSTGAME.

## Added

- `ImmersiveFrameworkQA.Camera.Runtime`
- `ImmersiveFrameworkQA.Camera.Editor`
- `FrameworkCameraDirector`
- `FrameworkRouteCameraBinding`
- `FrameworkActivityCameraBinding`
- `FrameworkCameraAnchorHost`
- `FrameworkCinemachineRigApplier`
- `QaCameraRouteActivityPanel`
- `QaCameraRouteActivitySceneBuilder`

## Configure

Run:

```text
Immersive Framework QA > Camera > Configure Route-Activity Camera QA
```

The configurator updates:

- `Assets/ImmersiveFrameworkQA/Scenes/StartupScene.unity`
- `Assets/ImmersiveFrameworkQA/Scenes/SecondScene.unity`

It creates scene-authored Route/Activity camera bindings and Cinemachine rigs for QA only.

## Expected smoke

Startup on `QA Canonical Route` should apply the Startup Activity camera directly.

Activity switch to the secondary activity should keep the retained Activity camera.

Route-fallback activity should return to the Route camera.

Switching to the alternate route should repeat the same pattern in `SecondScene`.

## Explicit cleanup from the discarded FIRSTGAME attempt

Remove these folders from the project if they are present from the previous incorrect patch:

```text
Assets/_Project/Scripts/Runtime/GameCamera
Assets/_Project/Scripts/Editor/Camera
```

The FIRSTGAME camera cut should only be recreated after this QA fixture passes.

## POST-RESET-H1E — Complete Camera Smoke Panel

The QA camera smoke panel was expanded into a scrollable, self-contained manual smoke surface.

Added panel sections:

- Smoke Navigation
- Expected Flow
- Director Diagnostics
- Trigger Diagnostics

The panel now shows the current effective camera rig, source, priority, route rig, activity rig, retained rig, active policy, and trigger outcomes. The configurator links the `FrameworkCameraDirector` to the panel and keeps the Camera QA panel focused on Camera-only route/activity controls.

## Update - F46D

The QA Camera smoke now consumes the official framework camera components directly:

- `FrameworkCameraDirector`
- `FrameworkRouteCameraBinding`
- `FrameworkActivityCameraBinding`
- `FrameworkCameraAnchorHost`
- `FrameworkCinemachineRigApplier`

The previous QA-only camera director, route binding, activity binding, anchor host, and activity camera policy were removed so the QA scene validates the package surface instead of duplicate QA runtime logic.

## Update — POST-RESET-H1F

The QA camera smoke panel now blocks manual activity/route requests when the linked trigger is missing its target asset. This prevents panel authoring mistakes from producing framework-level `Target Activity is missing` or `Target Route is missing` errors during the camera smoke.

The scene configurator also validates required object references immediately after assignment. If a QA camera binding, route trigger, or activity trigger cannot retain its assigned asset, the configurator emits a `[QA_CAMERA_SETUP]` error before Play Mode.
