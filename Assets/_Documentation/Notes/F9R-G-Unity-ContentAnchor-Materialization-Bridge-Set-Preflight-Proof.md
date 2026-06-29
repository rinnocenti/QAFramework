# F9R-G — Unity ContentAnchor Materialization Bridge Set Preflight Proof

Status: Implemented

## Summary

F9R-G hardens the authored `UnityContentAnchorMaterializationBridgeSet` batch operation with a preflight pass before any bridge materialization side effect is submitted.

The cut prevents partial batch materialization when a later bridge in the set is invalid.

## Runtime Files

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSet.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSetStatus.cs`

## QA

- `FrameworkQaCanvas` button: `Run Content Anchor Materialization Bridge Set Preflight Smoke`
- Smoke name: `Content Anchor Materialization Bridge Set Preflight Smoke`

## Validated Behavior

- A missing prefab in any authored bridge blocks the whole set before materialization side effects.
- Duplicate materialization keys across the set are blocked before materialization side effects.
- A valid set still materializes both bridges after preflight.
- Release all still releases physical objects, logical handles and logical ContentAnchor bindings.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto-materialization.
- No ContentAnchor object creation/destruction by the set itself.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay/camera/audio/save consumer.
