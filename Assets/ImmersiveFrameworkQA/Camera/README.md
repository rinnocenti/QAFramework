# Immersive Framework QA — Camera Route/Activity Cut

This QA fixture validates synthetic camera Route/Activity behavior inside the QA Harness.

The canonical Camera QA scene is also the destination for the C5 CameraComposer SinglePlayer smoke. Run `Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke` after entering this area from the QA Hub. The smoke materializes its isolated PlayerComposer/Cinemachine setup in this scene and is the primary Camera Product Surface proof.

Scope:

- Uses only `Assets/ImmersiveFrameworkQA/Camera` scripts.
- Configures QA scenes/assets under `Assets/ImmersiveFrameworkQA`.
- Does not create or depend on FIRSTGAME or `_Project` camera scripts.
- Validates Route camera, Startup Activity camera, Activity priority, retained Activity camera, and Route fallback.
- Entry scene: `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity`.
- Route/Activity camera smoke scenes: `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity` and `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_CameraRouteB.unity`.

Setup:

1. Ensure the Unity Cinemachine package is installed.
2. Run: `Immersive Framework QA > Camera > Configure Route-Activity Camera QA`.
   This recreates the Camera QA scenes from empty scenes and writes only Camera QA fixtures.
3. Start Play using the normal QA GameApplication.
4. Use the `QA Camera ...` panel to request Route/Activity switches.
   The panel is movable, scrollable, and contains smoke navigation, expected behavior, director diagnostics, and trigger diagnostics.

Expected priority:

```text
Activity Camera > Retained Activity Camera > Route Camera > Default Camera
```

Expected log prefix:

```text
[QA_CAMERA]
```

The former PlayerView Camera Activation QA remains a compatibility regression and is intentionally not a primary Hub entry; it validates the legacy `Camera.enabled` adapter contract.


## POST-RESET-H1F smoke panel guardrails

After applying this update, run:

```text
Immersive Framework QA > Camera > Configure Route-Activity Camera QA
```

The panel disables route/activity smoke buttons when a target asset is missing and reports the missing target in the trigger diagnostics. The configurator validates required references in both `Camera/Scenes/QA_Camera.unity` and `Camera/Scenes/QA_CameraRouteB.unity` so authoring mistakes are caught before Play Mode.
