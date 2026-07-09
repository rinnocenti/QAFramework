# Camera QA Cleanup and C5 Smoke Manifest

Status: QAFramework structural cleanup
Repository: `QAFramework`

## Objective

Create one clean Camera Product Surface scene for C5, demote the existing Route/Activity Camera scenes to Legacy / Diagnostic / Compatibility, and remove legacy camera activation from the primary Hub surface.

## Scope

- `Assets/ImmersiveFrameworkQA/Hub`
- `Assets/ImmersiveFrameworkQA/Camera`
- `Assets/ImmersiveFrameworkQA/Player` camera-related regression paths
- `Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring`
- `Assets/ImmersiveFrameworkQA/Documentation`

## Out of scope

- `Packages/com.immersive.framework`
- `planet-devourer` / FIRSTGAME
- project dependencies and `ProjectSettings`
- new Camera package/runtime architecture

## Files created

- `Assets/ImmersiveFrameworkQA/Documentation/CAMERA-QA-CLEANUP-AND-C5-SMOKE-MANIFEST.md`
- `Assets/ImmersiveFrameworkQA/Camera/Scripts/Runtime/QaCameraProductSurfaceFixture.cs`
- `Assets/ImmersiveFrameworkQA/Camera/Routes/QA_CameraProductSurfaceRoute.asset`
- `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera_ProductSurface.unity`

## Files changed

- `Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC5CameraComposerSinglePlayerSmoke.cs`
- `Assets/ImmersiveFrameworkQA/Hub/Scripts/Editor/QaHubSceneBuilder.cs`
- `Assets/ImmersiveFrameworkQA/Documentation/C5-CameraComposer-SinglePlayer-QA.md`
- `Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity` (Camera entry now points to Product Surface)
- `Assets/ImmersiveFrameworkQA/Camera/README.md`

## Files removed

- `Assets/ImmersiveFrameworkQA/Documentation/C5-CAMERA-COMPOSER-SINGLEPLAYER-QA-MANIFEST.md` (superseded by this structural manifest)

## Classification

| Area | Decision | Reason |
| --- | --- | --- |
| `Camera/Scenes/QA_Camera_ProductSurface.unity` | KEEP | Clean primary C5 Product Surface |
| `Camera/Scenes/QA_Camera.unity` and `QA_CameraRouteB.unity` | KEEP as legacy | Existing Route/Activity regressions remain useful but are not the current Product Surface |
| C5 editor smoke | KEEP_BUT_MOVE | Technical proof consumes the serialized clean-scene fixture |
| `PlayerView Camera Activation` route/scene/fixture | KEEP as compatibility regression | Still useful for the old adapter contract; no longer Product Surface flow |
| Legacy camera activation Hub entry | REMOVE FROM HUB | Prevents `Camera.enabled` semantics from masquerading as Cinemachine-first camera QA |
| Embedded C5 objects in `Hub/Scenes/QA_Hub.unity` | REMOVE | Duplicate and disconnected operational fixture |

## Expected usage flow

1. Open `Hub/Scenes/QA_Hub.unity`.
2. Enter `Camera Product Surface QA`.
3. Run `Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke`.
4. Inspect `QA_CameraProductSurface_Root` and its serialized fixture in `Camera/Scenes/QA_Camera_ProductSurface.unity`.
5. Confirm the Console PASS/FAIL diagnostics.

## Technical smoke

The smoke validates `Validate`, first `Apply/Rebuild`, second `Apply/Rebuild`, explicit `PlayerComposer.CameraTarget` and `LookAtTarget`, Cinemachine materialization, idempotency, and explicit blocking when `PlayerComposer` is missing. It does not use `Camera.main` or product name/path lookup.

## Acceptance criteria

Technical: QAFramework compiles; Hub reaches the new clean Camera Product Surface scene; C5 passes; second Apply/Rebuild creates zero objects and blocks zero; missing PlayerComposer fails explicitly; no official package or FIRSTGAME file changes.

Product: Camera QA has one clear operational area; users do not manually assemble C5 objects; legacy `Camera.enabled` QA is not a primary Hub path.

## Architectural and usability gains

The harness separates the current Camera Product Surface from the older Route/Activity diagnostic surface. C5 has one explicit Cinemachine-first entry point and a scene-authored fixture with typed references.

## Risks and manual validation

Unity scene serialization must be reimported and inspected after the cleanup. Do not claim compile or smoke PASS until Unity confirms import/compile and the manual flow above.

## Suggested commit message

`QA: clean camera harness and add CameraComposer smoke`
