# F9R-J — Unity ContentAnchor Materialization Diagnostics Snapshot Proof

Status: Closed / PASS

## Summary

F9R-J adds a query-only diagnostics snapshot for the authored `UnityContentAnchorMaterializationBridgeSet` surface.

The snapshot gives QA, Inspector and runtime diagnostics a common read model without submitting materialization, release, binding or physical placement side effects.

## Runtime Files

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSetDiagnosticsSnapshot.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSet.cs`

## Editor Files

- `Packages/com.immersive.framework/Editor/Authoring/UnityContentAnchorMaterializationBridgeSetEditor.cs`

## QA

- `FrameworkQaCanvas` button: `Run Content Anchor Materialization Diagnostics Snapshot Smoke`
- Smoke name: `Content Anchor Materialization Diagnostics Snapshot Smoke`

## Validated Behavior

- Initial snapshot reports valid authoring with zero registry entries, active entries, physical release requests and content handles.
- Snapshot creation is repeatable and stable before runtime side effects.
- After explicit `MaterializeAll`, snapshot reports the last materialization status and active registry state.
- After explicit `ReleaseAll`, snapshot reports the last release status, inactive registry state and physical release request count.
- Repeated snapshot creation after release remains stable.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto-materialization.
- No ContentAnchor object creation/destruction by the bridge set itself.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay/camera/audio/save consumer.


## Smoke Evidence

Validated by user-submitted smoke for `Content Anchor Materialization Diagnostics Snapshot Smoke`.

Accepted log fields:

- `passed='True'`
- `initialAuthoring='Succeeded'`
- `materializeAll='SucceededMaterializedAll'`
- `releaseAll='SucceededReleasedAll'`
- `snapshotQueryOnly='True'`
- `repeatedSnapshotStable='True'`
- `runtimeUsesAuthoringValidation='True'`
- `batchPreflight='True'`
- `noRuntimeSideEffects='True'`
- `automaticLifecycleWiring='False'`
- `routeActivityAutoMaterialization='False'`
- all consumer flags remained `False`.
