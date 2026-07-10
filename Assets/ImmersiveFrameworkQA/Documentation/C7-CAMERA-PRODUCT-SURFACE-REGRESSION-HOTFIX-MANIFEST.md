# C7 Camera Product Surface Regression Hotfix

Status: hotfix

## Objective

Fix the C7 regression runner and the C5 smoke integration after C7 exposed that the clean Camera Product Surface fixture could exist while its serialized target references were not resolved.

## Scope

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC5CameraComposerSinglePlayerSmoke.cs
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC7CameraProductSurfaceRegressionSmoke.cs
```

## Changes

- C5 now exposes `RunForRegression()` returning a boolean result.
- C7 now calls C5 directly instead of using `EditorApplication.ExecuteMenuItem`.
- C7 now reports PASS/FAIL based on C5 result instead of only reporting that C5 was invoked.
- C5 now resolves targets from explicit `PlayerComposer` serialized references when fixture references are missing.
- C5 repairs QA fixture references when properties exist.
- C5 keeps product behavior strict: no `Camera.main`, no runtime manager, no player auto-creation.

## Expected Result

```text
[QA][C7 Camera Product Surface] Regression started. Delegating to C5 CameraComposer SinglePlayer smoke.
[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.
[QA][C7 Camera Product Surface] PASS. C5 CameraComposer SinglePlayer regression passed.
```

## Commit Message

```text
QA: fix Camera Product Surface regression fixture resolution
```
