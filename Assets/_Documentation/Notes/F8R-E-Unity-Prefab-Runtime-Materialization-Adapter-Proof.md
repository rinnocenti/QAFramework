# F8R-E — Unity Prefab Runtime Materialization Adapter Proof

Status: Implemented

## Scope

This cut implements the first physical RuntimeContent adapter proof.

It adds explicit Unity adapter-side materialization and release code while keeping RuntimeContent core identity, handle, root and logical release contracts unchanged.

## Runtime files

- `Packages/com.immersive.framework/Runtime/RuntimeContent/UnityPrefabRuntimeMaterializationAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/UnityObjectRuntimeReleaseAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/UnityRuntimeMaterializedObjectRegistry.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/UnityRuntimeMaterializedObjectEvidence.cs`
- `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`

## Implemented behavior

- `UnityPrefabRuntimeMaterializationAdapter` implements `IRuntimeMaterializationAdapter`.
- The adapter accepts `RuntimeMaterializationRequest` values for resource type `UnityPrefab`.
- The adapter instantiates one explicit `GameObject` prefab/template through the Unity adapter boundary.
- `UnityRuntimeMaterializedObjectRegistry` stores local physical evidence by `RuntimeContentIdentity`.
- `UnityObjectRuntimeReleaseAdapter` implements `IRuntimeReleaseAdapter` and requests `Object.Destroy` for adapter-created objects.
- `FrameworkQaCanvas` exposes `Run Runtime Prefab Materialization Smoke`.

## Smoke coverage

The QA smoke validates:

- missing prefab fails before materialization;
- valid prefab/template materializes a `GameObject`;
- logical `RuntimeContentHandle` is applied as materialized through `RuntimeContentRuntime`;
- duplicate materialization for the same identity is blocked;
- physical release requests `Object.Destroy` through the Unity adapter;
- logical release unregisters the handle;
- double physical release is blocked;
- guardrails remain false for ContentAnchor placement, Addressables, pooling, actor spawn, PlayerInputManager join and gameplay/camera/audio/save consumers.

## Non-goals

- No ContentAnchor physical placement.
- No Addressables adapter.
- No pooling adapter.
- No scene unload/release adapter.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No gameplay, camera, audio or save consumer.
- No framework-owned service locator or singleton registry.

## Manual validation

Run from the QA canvas:

```text
Run Runtime Prefab Materialization Smoke
```

Expected result:

```text
QA Smoke completed. name='Runtime Prefab Materialization Smoke'.
```

The step log should include:

```text
missingPrefabBlocked='True'
duplicateBlocked='True'
physicalRelease='Succeeded'
logicalRelease='Succeeded'
doubleReleaseBlocked='True'
contentAnchorPlacement='False'
addressables='False'
pooling='False'
actorSpawn='False'
playerJoin='False'
gameplayConsumer='False'
cameraConsumer='False'
audioConsumer='False'
saveConsumer='False'
```
