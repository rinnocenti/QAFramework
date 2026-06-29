# F9R-H — Unity ContentAnchor Materialization Authoring Validation Proof

Status: Implemented

## Summary

F9R-H adds authoring validation for the explicit Unity ContentAnchor materialization bridge and bridge set surfaces introduced in F9R-E/F9R-F and hardened in F9R-G.

The cut validates authored configuration before runtime submission and keeps the bridge model opt-in. It does not wire materialization into Route or Activity lifecycle.

## Runtime Files

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationAuthoringValidator.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationAuthoringValidationResult.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationAuthoringValidationStatus.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSet.cs`

## Editor Files

- `Packages/com.immersive.framework/Editor/Validation/FrameworkAuthoringValidator.cs`
- `Packages/com.immersive.framework/Editor/Authoring/UnityContentAnchorMaterializationBridgeEditor.cs`
- `Packages/com.immersive.framework/Editor/Authoring/UnityContentAnchorMaterializationBridgeSetEditor.cs`

## QA

- `FrameworkQaCanvas` button: `Run Content Anchor Materialization Authoring Validation Smoke`
- Smoke name: `Content Anchor Materialization Authoring Validation Smoke`

## Validated Behavior

- A bridge with missing prefab/anchor Transform is blocked by authoring validation with no runtime materialization side effects.
- A bridge set with duplicate materialization keys is blocked before runtime submission.
- A valid bridge set passes validation.
- Authoring validation reports explicit guardrails: no lifecycle auto-wiring and no Route/Activity auto-materialization.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto-materialization.
- No scene mutation.
- No prefab or asset mutation.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay/camera/audio/save consumer.
