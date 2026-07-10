# C8B2 — Cinemachine Output Applier QA Manifest

Status: ready for Unity validation
Scope: QAFramework only

## Objective

Prove the C8B1 `CinemachineCameraOutput` contract and `FrameworkCinemachineOutputApplier` in an isolated QA smoke before connecting Route/Activity camera bindings to the new output model.

## Scope

- Creates an editor-only QA smoke.
- Builds temporary in-memory GameObjects only.
- Validates successful output application.
- Validates required/optional diagnostics.
- Validates explicit CinemachineBrain scope diagnostics.
- Confirms the new applier does not mutate `UnityEngine.Camera.enabled` or `GameObject.activeSelf`.

## Out of scope

- No scene assets.
- No FIRSTGAME changes.
- No Route/Activity binding migration.
- No `FrameworkCameraDirector` rewrite.
- No legacy removal.
- No runtime lifecycle authority.

## Files created

```text
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B2CinemachineOutputApplierSmoke.cs
Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC8B2CinemachineOutputApplierSmoke.cs.meta
Assets/ImmersiveFrameworkQA/Documentation/C8B2-CINEMACHINE-OUTPUT-APPLIER-QA-MANIFEST.md
Assets/ImmersiveFrameworkQA/Documentation/C8B2-CINEMACHINE-OUTPUT-APPLIER-QA-MANIFEST.md.meta
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

None. This is a technical QA smoke for the C8B1 output contract. `CameraComposer` remains the designer-facing Camera Product Surface.

## Expected flow

Run:

```text
Immersive Framework/QA/Camera/C8B2 Cinemachine Output Applier Smoke
```

Expected final log:

```text
[QA][C8B2 Cinemachine Output] PASS. Cinemachine output applier validates explicit outputs, applies priority/targets, and blocks/skips invalid output states.
```

## Smoke coverage

- Valid output applies priority, follow and look-at targets.
- Required missing output blocks.
- Optional missing output skips explicitly.
- Missing `CinemachineBrain` blocks.
- Multiple explicit brains block.
- Brain scope mismatch blocks.
- Required missing follow target blocks.
- `UnityEngine.Camera.enabled` is not changed.
- `GameObject.activeSelf` is not changed.

## Acceptance criteria — technical

```text
QAFramework compiles.
C8B2 smoke passes.
C7 Product Surface regression still passes.
No scene asset is created or changed.
No FIRSTGAME file is changed.
No Route/Activity runtime is changed.
No Camera.main or global lookup is introduced.
No singleton/manager is introduced.
No fallback silent behavior is introduced.
```

## Acceptance criteria — product

```text
CameraComposer remains the main Camera Product Surface.
The output applier remains a technical contract proof.
Route/Activity migration remains deferred until QA output behavior is proven.
```

## Suggested commit message

```text
QA: add Cinemachine output applier smoke
```
