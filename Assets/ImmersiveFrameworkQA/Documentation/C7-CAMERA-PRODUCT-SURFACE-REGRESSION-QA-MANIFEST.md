# C7 — Camera Product Surface Regression QA Manifest

## Objective

Create a formal QA regression entry point for the current Camera Product Surface proof chain.

## Scope

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring
Assets/ImmersiveFrameworkQA/Documentation
```

## Out Of Scope

```text
Packages/com.immersive.framework
FIRSTGAME / planet-devourer
new Camera contracts
Camera runtime authority
Route/Activity camera rewrite
multiplayer camera
legacy Camera.enabled validation
```

## Files Created

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC7CameraProductSurfaceRegressionSmoke.cs
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC7CameraProductSurfaceRegressionSmoke.cs.meta
Assets/ImmersiveFrameworkQA/Documentation/C7-Camera-Product-Surface-Regression-QA.md
Assets/ImmersiveFrameworkQA/Documentation/C7-Camera-Product-Surface-Regression-QA.md.meta
Assets/ImmersiveFrameworkQA/Documentation/C7-CAMERA-PRODUCT-SURFACE-REGRESSION-QA-MANIFEST.md
Assets/ImmersiveFrameworkQA/Documentation/C7-CAMERA-PRODUCT-SURFACE-REGRESSION-QA-MANIFEST.md.meta
```

## Files Changed

```text
None
```

## Files Removed

```text
None
```

## Product Surface Affected

```text
Camera Product Surface QA
CameraComposer SinglePlayer validation
Cinemachine rig materialization regression
```

## Expected Usage Flow

```text
1. Open QAFramework.
2. Run Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke.
3. The runner opens QA_Camera_ProductSurface if the current scene is clean.
4. The runner invokes the canonical C5 CameraComposer SinglePlayer smoke.
5. The C5 PASS/FAIL log is used as the authoritative result.
```

## Expected Technical Smoke

```text
[QA][C7 Camera Product Surface] Regression started. Delegating to C5 CameraComposer SinglePlayer smoke.
[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.
[QA][C7 Camera Product Surface] C5 smoke invoked. Use the C5 PASS/FAIL log as the authoritative regression result.
```

## Technical Acceptance Criteria

```text
QAFramework compiles.
C7 menu appears.
C7 opens the clean Camera Product Surface QA scene when safe.
C7 fails explicitly if the active scene is dirty.
C7 fails explicitly if the target scene is missing.
C7 fails explicitly if the C5 menu is missing.
C7 invokes the canonical C5 smoke.
C5 remains the authoritative technical result.
No package files are changed.
No FIRSTGAME files are changed.
```

## Product Acceptance Criteria

```text
Camera Product Surface QA has one obvious regression entry point.
The clean C5 scene remains canonical.
Legacy camera QA does not become the primary flow again.
The user does not need to remember the scene path before running the regression.
The proof chain is understandable: C5 proves QA technically; C6 proves FIRSTGAME usability.
```

## Architectural Gain

```text
Separates regression orchestration from product implementation.
Avoids creating a second Camera smoke contract.
Preserves C5 as the canonical technical proof.
Keeps FIRSTGAME proof out of QAFramework.
```

## Usability Gain

```text
One menu entry runs the current accepted Camera Product Surface regression.
The runner selects the correct scene.
Failure reasons are explicit.
Documentation explains the accepted proof chain.
```

## Risks

```text
If the C5 menu path changes, C7 must be updated.
If the C5 smoke becomes asynchronous later, C7 should be upgraded to consume a structured result instead of delegating through ExecuteMenuItem.
```

## Suggested Commit Message

```text
QA: add Camera Product Surface regression entry
```
