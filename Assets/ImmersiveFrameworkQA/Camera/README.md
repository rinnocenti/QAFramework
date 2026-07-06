# Immersive Framework QA — Camera Route/Activity Cut

This QA fixture validates the camera flow before the same pattern is consumed by FIRSTGAME.

Scope:

- Uses only `Assets/ImmersiveFrameworkQA/Camera` scripts.
- Configures QA scenes/assets under `Assets/ImmersiveFrameworkQA`.
- Does not create or depend on FIRSTGAME `_Project` camera scripts.
- Validates Route camera, Startup Activity camera, Activity priority, retained Activity camera, and Route fallback.

Setup:

1. Ensure the Unity Cinemachine package is installed.
2. Run: `Immersive Framework QA > Camera > Configure Route-Activity Camera QA`.
3. Start Play using the normal QA GameApplication.
4. Use the `QA Camera ...` panel to request Route/Activity switches.
   The panel is scrollable and contains smoke navigation, expected behavior, director diagnostics, trigger diagnostics, and related-panel toggles.

Expected priority:

```text
Activity Camera > Retained Activity Camera > Route Camera > Default Camera
```

Expected log prefix:

```text
[QA_CAMERA]
```


## POST-RESET-H1F smoke panel guardrails

After applying this update, run:

```text
Immersive Framework QA > Camera > Configure Route-Activity Camera QA
```

The panel disables route/activity smoke buttons when a target asset is missing and reports the missing target in the trigger diagnostics. The configurator validates required references in both `StartupScene` and `SecondScene` so authoring mistakes are caught before Play Mode.
