# C7 — Camera Product Surface Regression Enum Hotfix

Status: hotfix
Date: 2026-07-10
Scope: QAFramework only

## Objective

Remove noisy `enum index is out of range` exceptions from the C5/C7 Camera Product Surface regression smoke.

## Issue

The smoke configured enum serialized properties with `SerializedProperty.enumValueIndex = Convert.ToInt32(value)`. That is unsafe because framework enums may use explicit numeric values that do not match Unity serialized enum option indices.

## Change

`QaC5CameraComposerSinglePlayerSmoke.SetEnum` now resolves enum values by serialized enum name first, then uses a guarded index fallback, then writes the numeric value only as a last resort.

## Files Changed

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC5CameraComposerSinglePlayerSmoke.cs
```

## Expected Result

Running:

```text
Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke
```

should produce no `enum index is out of range` exceptions and should end with:

```text
[QA][C7 Camera Product Surface] PASS. C5 CameraComposer SinglePlayer regression passed.
```

## Commit Message

```text
QA: fix CameraComposer regression enum serialization
```
