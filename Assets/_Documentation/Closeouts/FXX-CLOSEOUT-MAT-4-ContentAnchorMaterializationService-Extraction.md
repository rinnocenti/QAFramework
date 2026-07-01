# FXX-CLOSEOUT - MAT-4 ContentAnchorMaterializationService Extraction

Status: Implemented / pending Unity validation
Date: 2026-07-01

## 1. Service created

MAT-4 adds `ContentAnchorMaterializationService` as the reusable non-MonoBehaviour entry point for the RuntimeContent + ContentAnchor materialization operation.

The service owns the sequence:

```text
RuntimeMaterializationRequest creation or supplied request validation
physical materialization through UnityPrefabRuntimeMaterializationAdapter
RuntimeContentRuntime materialization apply
materialized Unity evidence validation
logical ContentAnchor binding
physical placement under the authored anchor Transform
physical/logical rollback when a post-materialization stage fails
```

Supporting internal result types were added:

```text
ContentAnchorMaterializationStage
ContentAnchorMaterializationResult
ContentAnchorMaterializationRollbackResult
```

The aggregate result reports `failedStage`, preserves original subsystem results, and carries rollback evidence without adding a universal status enum.

## 2. Responsibilities migrated

`UnityContentAnchorMaterializationPipeline` no longer coordinates the materialize -> apply -> evidence -> bind -> place -> rollback path itself.
It now delegates to `ContentAnchorMaterializationService` and maps the service result back to the existing pipeline result/status type for compatibility.

`UnityContentAnchorMaterializationBridge` no longer instantiates the pipeline or coordinates the full path.
It remains responsible for:

```text
Inspector serialized fields
authoring/configuration validation
runtime context and binding request construction
service invocation
bridge diagnostics/result exposure
scope release command
```

Serialized fields and public bridge methods were preserved.

## 3. Rollback preserved

Rollback still uses `ContentAnchorReleaseExecution` for the physical release -> logical release order.

Failure rollback coverage remains:

```text
RuntimeContent apply failure: physical/logical release rollback.
Missing materialized evidence: physical/logical release rollback.
Logical binding failure: physical/logical release rollback.
Physical placement failure: logical ContentAnchor unbind, then physical/logical release rollback.
```

The rollback result exposes:

```text
attempted
succeeded
bindingUnbound
physical release result
logical release result
```

## 4. Diagnostics preserved

The bridge keeps the existing `UnityContentAnchorMaterializationBridgeResult` shape and maps service failed stages back to the previous `UnityContentAnchorMaterializationPipelineStatus` values for observable smoke parity.

The pipeline keeps its public result/status contract and remains as a compatibility wrapper.

## 5. Boundary preserved

Package/module owner:

```text
Package: com.immersive.framework
Module: Runtime/ContentAnchor with RuntimeContent collaboration
```

No technical frozen package was changed.
No RouteLifecycle or ActivityFlow path was changed.
No pooling, actor, pause, loading, transition, camera, audio, save, Addressables, service locator, singleton, reflection or generic Saga was introduced.
`RuntimeContentRuntime` was not split in this cut.

## 6. Validation performed

Static implementation review only in this turn.

No Unity compile/import, playmode, batchmode or smoke was run.

## 7. Manual validation checklist

1. Unity compile/import
2. Composite Lifecycle Release Smoke
3. Content Anchor Diagnostics Smoke
4. Activity Content Anchor Diagnostics Smoke
5. Standard Smoke
6. Content Anchor materialization smoke if present in QA Canvas

## 8. Remaining risks

Real Unity side effects still require smoke parity validation in-editor.
The old pipeline remains as a compatibility wrapper; a future cut can decide whether to retire it after downstream callers are reviewed.
The service currently targets the existing Unity prefab/materialized-object registry adapters; broader adapter generalization should wait for a second concrete materialization use case.

## 9. SOLID impact

Single responsibility improved: the bridge is authoring/diagnostics, the pipeline is compatibility, and the service owns orchestration.
Dependency direction remains explicit through constructor-injected adapters.
No global lookup or hidden fallback was added.
