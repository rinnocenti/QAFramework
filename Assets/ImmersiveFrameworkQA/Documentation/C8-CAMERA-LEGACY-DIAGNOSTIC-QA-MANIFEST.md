# C8A — Camera Legacy Diagnostic QA Manifest

Status: QAFramework C8A boundary

## Current product path

The primary Camera QA is `Camera Product Surface QA`, using `QA_Camera_ProductSurface.unity` and the C7 Camera Product Surface Regression Smoke. It validates the official `CameraComposer` / `PlayerComposer` / Cinemachine path.

## Legacy diagnostic path

The following remain technical regressions only:

- `Camera/Scenes/QA_Camera.unity`
- `Camera/Scenes/QA_CameraRouteB.unity`
- `Camera/Scripts/Runtime/QaCameraRouteActivityPanel.cs`
- `Camera/Scripts/Editor/QaCameraRouteActivitySceneBuilder.cs`
- `PlayerView Camera Activation QA`
- Route/Activity bindings using `FrameworkCameraDirector`, `FrameworkRouteCameraBinding`, `FrameworkActivityCameraBinding` and `FrameworkCameraAnchorHost`

These scenes and scripts are not the Camera Product Surface and must not be described as the designer-first camera workflow.

## C8A actions

- Removed legacy Camera/PlayerView flows from the primary Hub Product Surface entry.
- Preserved Route/Activity scenes and fixtures as Diagnostic/Compatibility regressions.
- Kept the clean C7 scene isolated from Route/Activity panels, resets and historical fixtures.
- Documented Cinemachine and CameraComposer as the current official direction.

## Out of scope

- Runtime Route/Activity rewrite.
- Multiplayer, split-screen, shared group, spectator and advanced transitions.
- Changes to the official package runtime or FIRSTGAME scenes.

## Validation

Run the primary C7 smoke first. Run legacy Route/Activity QA only when validating lifecycle compatibility. A legacy PASS does not constitute Camera Product Surface PASS.

## Suggested commit message

`QA: separate Camera Product Surface from legacy camera diagnostics`
