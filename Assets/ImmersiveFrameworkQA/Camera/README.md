# Immersive Framework QA — Camera Route/Activity Cut

The primary Camera Product Surface QA is `Scenes/QA_Camera_ProductSurface.unity`. It is a clean, isolated C5 harness for the current Cinemachine-first CameraComposer MVP.

`Scenes/QA_Camera.unity` and `Scenes/QA_CameraRouteB.unity` are retained as Legacy / Diagnostic / Compatibility Route/Activity camera QA. They contain older panels, reset controls, activity bindings and route fixtures and are not the primary Camera Product Surface flow.

Run `Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke` after entering `Camera Product Surface QA` from the QA Hub. The smoke consumes the serialized `QaCameraProductSurfaceFixture` and materializes its isolated PlayerComposer/Cinemachine setup in the clean scene.

Scope:

- Uses only `Assets/ImmersiveFrameworkQA/Camera` scripts.
- Configures QA scenes/assets under `Assets/ImmersiveFrameworkQA`.
- Does not create or depend on FIRSTGAME or `_Project` camera scripts.
- Validates Route camera, Startup Activity camera, Activity priority, retained Activity camera, and Route fallback.
- Legacy Route/Activity entry scenes: `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity` and `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_CameraRouteB.unity`.
- Route/Activity camera smoke scenes: `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity` and `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_CameraRouteB.unity`.

Legacy Route/Activity setup:

1. Ensure the Unity Cinemachine package is installed.
2. Run: `Immersive Framework QA > Camera > Configure Route-Activity Camera QA`.
   This recreates the Camera QA scenes from empty scenes and writes only Camera QA fixtures.
3. Start Play using the normal QA GameApplication when validating the legacy Route/Activity surface.
4. Use the `QA Camera ...` panel to request Route/Activity switches.
   The panel is movable, scrollable, and contains smoke navigation, expected behavior, director diagnostics, and trigger diagnostics.

Expected priority:

```text
Activity Camera > Retained Activity Camera > Route Camera > Default Camera
```

Expected C5 PASS log:

```text
[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.
```

The C5 smoke proves explicit PlayerComposer anchors, Cinemachine materialization, Apply/Rebuild idempotency and explicit missing-PlayerComposer blocking. It does not prove RouteCamera, ActivityCamera, multiplayer, FIRSTGAME usability or runtime camera authority.

The former PlayerView Camera Activation QA remains a compatibility regression and is intentionally not a primary Hub entry; it validates the legacy `Camera.enabled` adapter contract.


## POST-RESET-H1F smoke panel guardrails

After applying this update, run:

```text
Immersive Framework QA > Camera > Configure Route-Activity Camera QA
```

The panel disables route/activity smoke buttons when a target asset is missing and reports the missing target in the trigger diagnostics. The configurator validates required references in both `Camera/Scenes/QA_Camera.unity` and `Camera/Scenes/QA_CameraRouteB.unity` so authoring mistakes are caught before Play Mode.

The clean C5 surface does not use this panel. It is entered through the Hub and validated by the editor smoke against the serialized scene fixture.
