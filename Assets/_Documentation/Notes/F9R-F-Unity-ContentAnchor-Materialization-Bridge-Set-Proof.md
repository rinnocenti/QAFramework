# F9R-F — Unity ContentAnchor Materialization Bridge Set Proof

Status: Implemented

## Summary

F9R-F adds an authored, opt-in bridge set for explicitly submitting multiple `UnityContentAnchorMaterializationBridge` instances as one operation.

The cut moves from a single authored bridge proof to a small authored set proof without adding automatic Route/Activity lifecycle wiring.

## Runtime Files

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSet.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSetResult.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSetStatus.cs`

## QA

- `FrameworkQaCanvas` button: `Run Content Anchor Materialization Bridge Set Smoke`
- Smoke name: `Content Anchor Materialization Bridge Set Smoke`

## Validated Behavior

- Missing bridge set blocks before materialization.
- Two explicit authored bridges materialize in one submitted operation.
- Duplicate materialization is blocked without new active content.
- Explicit release releases both bridge items.
- Repeated release is idempotent.
- Materialization after release remains blocked.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto-materialization.
- No ContentAnchor object creation/destruction by the set itself.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay/camera/audio/save consumer.
