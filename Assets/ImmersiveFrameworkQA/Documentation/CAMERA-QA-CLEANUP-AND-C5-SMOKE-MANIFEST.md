# Camera QA Cleanup and C5 Smoke Manifest

Status: QAFramework structural cleanup
Repository: `QAFramework`

## Objective

Keep one canonical Camera QA scene, move the C5 CameraComposer smoke into that scene's operational flow, and remove legacy camera activation from the primary Hub surface.

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

## Files changed

- `Assets/ImmersiveFrameworkQA/Editor/CameraAuthoring/QaC5CameraComposerSinglePlayerSmoke.cs`
- `Assets/ImmersiveFrameworkQA/Hub/Scripts/Editor/QaHubSceneBuilder.cs`
- `Assets/ImmersiveFrameworkQA/Documentation/C5-CameraComposer-SinglePlayer-QA.md`
- `Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity` (remove stale embedded C5 fixture)
- `Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity` (operational destination for C5)

## Files removed

- `Assets/ImmersiveFrameworkQA/Documentation/C5-CAMERA-COMPOSER-SINGLEPLAYER-QA-MANIFEST.md` (superseded by this structural manifest)

## Classification

| Area | Decision | Reason |
| --- | --- | --- |
| `Camera/Scenes/QA_Camera.unity` | KEEP | Existing canonical Camera route scene and C5 destination |
| C5 editor smoke | KEEP_BUT_MOVE | Technical proof is useful, but must run in the canonical Camera scene |
| `PlayerView Camera Activation` route/scene/fixture | KEEP as compatibility regression | Still useful for the old adapter contract; no longer Product Surface flow |
| Legacy camera activation Hub entry | REMOVE FROM HUB | Prevents `Camera.enabled` semantics from masquerading as Cinemachine-first camera QA |
| Embedded C5 objects in `Hub/Scenes/QA_Hub.unity` | REMOVE | Duplicate and disconnected operational fixture |

## Expected usage flow

1. Open `Hub/Scenes/QA_Hub.unity`.
2. Enter `Camera QA`.
3. Run `Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke`.
4. Inspect the generated `QA_C5_CameraComposer_SinglePlayer_Smoke` root in `Camera/Scenes/QA_Camera.unity`.
5. Confirm the Console PASS/FAIL diagnostics.

## Technical smoke

The smoke validates `Validate`, first `Apply/Rebuild`, second `Apply/Rebuild`, explicit `PlayerComposer.CameraTarget` and `LookAtTarget`, Cinemachine materialization, idempotency, and explicit blocking when `PlayerComposer` is missing.

## Acceptance criteria

Technical: QAFramework compiles; Hub reaches the existing Camera scene; C5 passes; second Apply/Rebuild creates zero objects and blocks zero; missing PlayerComposer fails explicitly; no duplicate Camera scene exists; no official package or FIRSTGAME file changes.

Product: Camera QA has one clear operational area; users do not manually assemble C5 objects; legacy `Camera.enabled` QA is not a primary Hub path.

## Architectural and usability gains

The harness has one Camera owner scene and one explicit Cinemachine-first C5 entry point. Technical regressions remain available without competing with the current product surface.

## Risks and manual validation

Unity scene serialization must be reimported and inspected after the cleanup. Do not claim compile or smoke PASS until Unity confirms import/compile and the manual flow above.

## Suggested commit message

`QA: clean camera harness and add CameraComposer smoke`
