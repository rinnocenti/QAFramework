# F9R-L — Unity ContentAnchor Materialization Bridge Set Rollback Proof

Status: Closed / PASS

## Summary

F9R-L hardens `UnityContentAnchorMaterializationBridgeSet` after the F8R-F readiness review identified partial batch materialization as the most concrete blocker.

Before this cut, preflight prevented invalid authored batches before side effects, but a runtime failure after one or more bridges had already materialized could leave a partial active batch.

This cut adds explicit rollback for already materialized bridges when a later bridge fails during `MaterializeAll`.

## Runtime Files

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSet.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationBridgeSetStatus.cs`
- `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`

## Runtime Behavior

`UnityContentAnchorMaterializationBridgeSet.MaterializeAll` now behaves as follows:

1. Validate bridge references.
2. Run authoring validation.
3. Run bridge-set preflight.
4. Materialize bridges sequentially.
5. If bridge `N` fails after bridge `0..N-1` succeeded, release the already materialized bridges in reverse order.
6. Return an explicit rollback status.

New statuses:

- `FailedBridgeMaterializationRolledBack`
- `FailedBridgeMaterializationRollbackFailed`

## QA

New QA Canvas button:

- `Run Content Anchor Materialization Bridge Set Rollback Smoke`

Smoke name:

- `Content Anchor Materialization Bridge Set Rollback Smoke`

Expected successful log includes:

```text
step='unity-content-anchor-materialization-bridge-set-rollback'
passed='True'
materializeAll='FailedBridgeMaterializationRolledBack'
materialized='1'
rollbackReleased='1'
failed='1'
rollbackRequestedPhysicalRelease='True'
firstLogicalStateRolledBack='True'
partialMaterializationRolledBack='True'
preExistingPreserved='True'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
```

## Validated Scenario

The smoke constructs this case:

1. Bridge `2` is explicitly materialized before the batch.
2. The bridge set then attempts to materialize bridge `1` and bridge `2`.
3. Bridge `1` materializes successfully.
4. Bridge `2` fails because its own runtime content is already active.
5. The bridge set rolls back bridge `1`.
6. The pre-existing bridge `2` content remains active until an explicit release call.

This proves rollback is scoped to the partial batch and does not destroy pre-existing active content.

## Smoke Correction

The first smoke version incorrectly required the rolled-back Unity instance to disappear immediately from the hierarchy after `Object.Destroy` was requested. In Unity, `Destroy` is deferred until the end of the frame, so the correct immediate evidence is:

- the rolled-back bridge registry is inactive;
- the rolled-back runtime content handle count is zero;
- the rolled-back bridge has `PhysicalReleaseRequestedCount == 1`;
- the pre-existing second bridge remains active and parented.

The smoke now validates logical rollback plus physical release request instead of requiring immediate physical disappearance.

## Smoke Validation

Validated by user-provided Unity smoke after the smoke criterion correction.

Observed PASS fields:

```text
QA Content Anchor Materialization Bridge Set Rollback Smoke step completed.
step='unity-content-anchor-materialization-bridge-set-rollback'
passed='True'
preExisting='SucceededMaterialized'
materializeAll='FailedBridgeMaterializationRolledBack'
materialized='1'
rollbackReleased='1'
failed='1'
releaseAll='SucceededReleasedAll'
contentHandles='0'
rollbackAttempted='True'
rollbackRequestedPhysicalRelease='True'
firstLogicalStateRolledBack='True'
partialMaterializationRolledBack='True'
preExistingPreserved='True'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
bridgeSetCreatesObject='False'
bridgeSetDestroysObject='False'
addressables='False'
pooling='False'
actorSpawn='False'
playerJoin='False'
gameplayConsumer='False'
cameraConsumer='False'
audioConsumer='False'
saveConsumer='False'
```

Closure reading:

- rollback is proven for partial batch materialization;
- the pre-existing active bridge content is preserved;
- `releaseAll` clears the pre-existing bridge content explicitly after the rollback scenario;
- the proof remains authored/QA-only and does not create lifecycle auto-wiring.

## Non-goals

- No automatic lifecycle wiring.
- No Route/Activity auto-materialization.
- No lifecycle-owned registry.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay/camera/audio/save consumer.
