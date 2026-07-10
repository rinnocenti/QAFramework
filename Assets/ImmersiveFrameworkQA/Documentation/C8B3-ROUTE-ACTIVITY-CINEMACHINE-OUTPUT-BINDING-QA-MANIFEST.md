# C8B3 — Route/Activity Cinemachine Output Binding QA Manifest

Status: ready for Unity validation
Scope: QAFramework only

## Objective

Prove the C8B1 Cinemachine output contract in a Route/Activity-style lifecycle simulation before connecting it to the real `FrameworkCameraDirector`, `FrameworkRouteCameraBinding` or `FrameworkActivityCameraBinding` runtime path.

## Scope

Created an editor-only QA smoke that builds temporary hidden Cinemachine fixtures, applies explicit `CinemachineCameraOutput` values through `FrameworkCinemachineOutputApplier`, and validates lifecycle-like priority behavior.

## Out of scope

- No package runtime changes.
- No scene or asset changes.
- No FIRSTGAME changes.
- No `FrameworkCameraDirector` rewrite.
- No `FrameworkRouteCameraBinding` or `FrameworkActivityCameraBinding` migration.
- No removal of legacy Route/Activity camera behavior.
- No multiplayer, split-screen, group camera, spectator camera or advanced blending.

## Files created

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B3RouteActivityCinemachineOutputBindingSmoke.cs
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B3RouteActivityCinemachineOutputBindingSmoke.cs.meta
Assets/ImmersiveFrameworkQA/Documentation/C8B3-ROUTE-ACTIVITY-CINEMACHINE-OUTPUT-BINDING-QA-MANIFEST.md
Assets/ImmersiveFrameworkQA/Documentation/C8B3-ROUTE-ACTIVITY-CINEMACHINE-OUTPUT-BINDING-QA-MANIFEST.md.meta
```

## Files altered

```text
none
```

## Files removed

```text
none
```

## Product surface affected

None. This is a technical QA step. `CameraComposer` remains the official Camera Product Surface.

## Flow validated

```text
Route enter -> Route output applied
Activity enter -> Activity output priority overrides Route
Activity exit -> Activity output cleared and Route output restored
Activity UseRoute -> Route remains active without Activity override
Retained Activity -> retained output outranks Route
Missing required Activity output -> blocked diagnostic
Missing optional Activity output -> skipped diagnostic
Wrong brain scope -> blocked diagnostic
```

The smoke also asserts that the new path does not mutate `UnityEngine.Camera.enabled` or `GameObject.activeSelf`.

## Menu

```text
Immersive Framework/QA/Camera/C8B3 Route Activity Cinemachine Output Binding Smoke
```

## Expected smoke

```text
[QA][C8B3 RouteActivity Cinemachine Output] PASS. Route/Activity Cinemachine output binding applies Route, Activity override, Activity clear, UseRoute, retained Activity, and missing-output diagnostics without mutating legacy camera state.
```

## Required regression after this smoke

```text
Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke
```

Expected:

```text
[QA][C7 Camera Product Surface] PASS. C5 CameraComposer SinglePlayer regression passed.
```

## Acceptance criteria — technical

```text
QAFramework compiles.
C8B3 smoke passes.
C7 Camera Product Surface regression still passes.
No package runtime changed.
No FIRSTGAME changed.
No scene changed.
No Camera.main introduced.
No global lookup introduced.
No singleton/manager introduced.
No GameObject.SetActive path introduced in the new smoke behavior.
No Unity Camera.enabled mutation introduced.
```

## Acceptance criteria — product

```text
CameraComposer remains the designer-facing Product Surface.
Route/Activity remains technical integration.
Legacy diagnostics stay separate from Product Surface QA.
```

## Architectural gain

The framework now has evidence that C8B1 output contracts can express Route/Activity precedence semantics through explicit Cinemachine priority without relying on raw Camera.enabled or GameObject activation.

## Usability gain

C8B3 reduces risk before the real Route/Activity binding migration because the expected lifecycle semantics are now captured in a small, repeatable QA smoke.

## Suggested commit message

```text
QA: add Route Activity Cinemachine output binding smoke
```
