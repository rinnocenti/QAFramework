# F10C — Pause ContentAnchor Binding Request Proof

Status: Closed / PASS

## Purpose

F10B created a passive authored Pause visual surface contract. F10C proves the next smallest step: that this Pause contract can produce an explicit `ContentAnchorBindingRequest`.

This is still request-only. It does not execute binding, materialize a prefab, place a transform, release content, switch input, change `Time.timeScale` or wire into Route/Activity lifecycle.

## Why this cut exists

A ContentAnchor binding is not only an anchor id. The framework needs two identities before it can bind anything safely:

```text
where should content go?  -> ContentAnchor identity
what content is being put? -> RuntimeContent identity
```

The Pause visual surface already had:

- runtime owner;
- runtime content id;
- materialization resource;
- anchor scope/kind/id.

F10C adds the missing authored piece required for a canonical binding request:

```text
ContentAnchor owner
```

Without the owner, two anchors with the same id in different scopes/owners could be ambiguous.

## Implemented changes

Runtime Pause contracts:

- `PauseVisualSurfaceContract` now records `AnchorOwner`.
- `PauseVisualSurfaceAuthoring` creates the anchor owner key from the authored anchor scope and owner id.
- `PauseVisualSurfaceBindingRequestFactory` derives a passive `ContentAnchorBindingRequest` from a valid Pause visual surface contract.
- `PauseVisualSurfaceBindingRequestResult` records success/failure without side effects.
- `PauseVisualSurfaceBindingRequestStatus` records request derivation status.

QA:

- Added `Run Pause Content Anchor Binding Request Smoke`.

Guide:

- Added `Packages/com.immersive.framework/Documentation~/Guides/F10C-Pause-ContentAnchor-Binding-Usage.md`.

## Validated smoke

```text
QA Pause Content Anchor Binding Request Smoke step completed.
step='pause-content-anchor-binding-request'
passed='True'
contract='True'
bindingRequest='SucceededCreated'
mismatchedContext='RejectedMismatchedRuntimeOwner'
runtimeScope='Transient'
runtimeOwner='Transient:Runtime:qa.pause.binding-request.owner'
runtimeContentId='qa.pause.binding-request.content'
runtimeIdentity='Transient:Runtime:qa.pause.binding-request.owner:qa.pause.binding-request.content'
anchorScope='Local'
anchorOwner='Local:qa.pause.binding-request.anchor-owner'
anchorKind='Root'
anchorId='qa.pause.binding-request.overlay'
resourceRecorded='True'
requestMatchesPauseContract='True'
requestMatchesAnchorRequirement='True'
anchorOwnerRecorded='True'
mismatchedContextRejected='True'
requestOnly='True'
bindingExecution='False'
materialization='False'
physicalRelease='False'
logicalRuntimeContentRelease='False'
contentAnchorBindingCleanup='False'
inputModeChange='False'
timeScalePolicy='False'
explicitSubmit='True'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
addressables='False'
pooling='False'
actorSpawn='False'
playerJoin='False'
gameplayConsumer='False'
cameraConsumer='False'
audioConsumer='False'
saveConsumer='False'
```

## Boundaries preserved

F10C does not implement:

- visual Pause materialization;
- logical `RuntimeContentAnchorBinding.Bind` execution for Pause;
- physical placement;
- lifecycle registry registration;
- composite release;
- InputMode or PlayerInput changes;
- `Time.timeScale` policy;
- Route/Activity auto-release;
- Route/Activity auto-materialization;
- gameplay/F34;
- camera, audio, save, actor, pooling or PlayerJoin consumers.

## Next candidate

`F10D — Pause ContentAnchor Binding Execution Proof`.

That next cut should prove explicit logical binding execution for Pause after runtime content has been explicitly registered, still without visual materialization or Pause toggle integration.
