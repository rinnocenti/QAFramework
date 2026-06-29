# F9R-E — Unity ContentAnchor Materialization Bridge Proof

Status: Implemented

## Summary

This cut adds an authored, opt-in Unity bridge over the validated ContentAnchor materialization pipeline.

The bridge is explicit scene-facing runtime code. It does not register automatic Route/Activity lifecycle wiring and does not select gameplay, camera, audio, save, pooling or actor materialization consumers.

## Runtime Added

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeResult.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeStatus.cs`

## QA Added

- `FrameworkQaCanvas` button: `Run Content Anchor Materialization Bridge Smoke`

## Proven Path

```text
Authored bridge explicit call
  -> RuntimeContent owner/context
  -> Unity prefab materialization pipeline
  -> logical ContentAnchor binding
  -> physical anchor placement
  -> explicit scope release
```

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto materialization.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No camera/audio/save/gameplay consumer.
- No Unity name/path as functional identity.

## Expected Smoke

Run from QA Canvas:

```text
Run Content Anchor Materialization Bridge Smoke
```

Expected completion:

```text
QA Smoke completed. name='Content Anchor Materialization Bridge Smoke'.
```
