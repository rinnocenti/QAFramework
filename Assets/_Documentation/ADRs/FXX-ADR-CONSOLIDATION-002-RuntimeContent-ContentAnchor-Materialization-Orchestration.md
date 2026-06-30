# FXX-ADR-CONSOLIDATION-002 — RuntimeContent / ContentAnchor Materialization Orchestration

Status: Proposed / audit-first / refactor governance  
Fase: candidata — número de fase a definir pelo roadmap  
Tipo: Core / RuntimeContent / ContentAnchor / Refactor Governance  
Última atualização: 2026-06-30

This ADR does not authorize implementation by itself. It defines the intended direction for a future plan.

---

## 1. Context

The framework now has the primitives needed for runtime content placement:

```text
RuntimeContent root/context/handle/materialization/release
ContentAnchor declaration/set/binding
Unity prefab materialization adapter
Unity object release adapter
Unity anchor placement adapter
Unity bridge for authored smoke/manual execution
```

These pieces are conceptually correct. The problem is composition: the reusable operation “materialize a prefab, bind it logically to a ContentAnchor and place it physically under an anchor Transform” is currently coordinated by Unity-facing infrastructure instead of by a reusable orchestration boundary.

The same class of problem was found in the Participant consolidation audit: repeated or hard-to-reuse orchestration logic grows when a project has strong contracts but no shared composition layer.

---

## 2. Decision

Create a future implementation phase that extracts a reusable orchestration boundary:

```text
ContentAnchorMaterializationService
```

The service owns the sequence:

```text
1. create RuntimeMaterializationRequest;
2. materialize physical Unity content through adapter;
3. apply RuntimeContent materialization result;
4. validate materialized evidence;
5. bind logical ContentAnchor handle;
6. place physical content under the anchor Transform;
7. rollback physical/logical state on failure.
```

`UnityContentAnchorMaterializationBridge` should become a thin authoring/QA wrapper:

```text
Inspector fields -> authoring validation -> service call -> diagnostics/result display.
```

---

## 3. Scope included

```text
Runtime/ContentAnchor/ContentAnchorMaterializationService.cs
Runtime/ContentAnchor/ContentAnchorMaterializationRequest.cs       (if needed)
Runtime/ContentAnchor/ContentAnchorMaterializationResult.cs        (aggregate result)
Runtime/ContentAnchor/ContentAnchorMaterializationStage.cs         (CreatedRequest/Materialized/Applied/Bound/Placed/Rollback)
Runtime/ContentAnchor/ContentAnchorMaterializationRollbackResult.cs (if needed)
UnityContentAnchorMaterializationBridge delegates to the service.
UnityContentAnchorMaterializationPipeline is either removed, reduced, or wrapped by the service depending on lowest-risk migration.
```

The first implementation cut may keep existing pipeline/result/status types internally if that preserves smoke parity. The architectural requirement is that the reusable entry point is no longer the MonoBehaviour bridge.

---

## 4. Scope excluded

```text
No pooling integration.
No actor materialization integration.
No pause/loading/transition consumer integration.
No public Inspector field changes.
No change to ContentAnchor identity, scope, kind or requiredness.
No change to RuntimeContentHandle public semantics.
No broad split of RuntimeContentRuntime in the first service extraction cut.
No generic Saga/CompensatingStepRunner unless a second use case is approved.
```

---

## 5. Result/status policy

The aggregate result should prefer:

```text
FailedStage + original stage result + rollback result
```

instead of repeatedly remapping every sub-result into new semantic enums.

Allowed:

```text
ContentAnchorMaterializationStage.FailedPlacement
OriginalPlacementResult = UnityContentAnchorPlacementResult
RollbackResult = ContentAnchorMaterializationRollbackResult
```

Avoid:

```text
RuntimeMaterializationStatus -> PipelineStatus -> BridgeStatus -> another consumer status
```

The goal is not to remove all status enums. The goal is to avoid hiding the original failing subsystem.

---

## 6. RuntimeContentRuntime policy

`RuntimeContentRuntime` is broad and should eventually be split by responsibility. This ADR does not authorize that split immediately.

Future split candidates:

```text
RuntimeScopeRootManager
RuntimeContentHandleRegistry
RuntimeMaterializationRequestFactory
RuntimeMaterializationStateApplier
RuntimeReleaseCoordinator
```

The service extraction should happen first. The split should happen only after the service boundary reveals the stable seams.

---

## 7. Compatibility

The migration must preserve:

```text
- prefab instantiation behavior;
- anchor parenting behavior;
- reset local transform behavior;
- registry counts used by diagnostics;
- handle state transitions;
- current bridge public methods;
- current serialized fields;
- current QA smoke output unless explicitly documented as a bug fix.
```

---

## 8. Consequences

### Positive

- Future materialization consumers can call a reusable service instead of copying the bridge.
- Bridge becomes easier to explain and safer as Inspector-facing code.
- Failure diagnostics become stage-oriented.
- RuntimeContentRuntime split can be planned from real seams instead of guessed upfront.

### Negative / cost

- Adds one more orchestration abstraction.
- Requires careful parity testing because side effects are real Unity side effects.
- May temporarily keep old pipeline types while introducing the service, increasing code count before closeout.

---

## 9. Acceptance criteria for implementation phase

```text
1. Existing materialization smoke passes with identical observable output.
2. UnityContentAnchorMaterializationBridge no longer manually constructs the whole orchestration path.
3. A non-MonoBehaviour caller can invoke the materialization service.
4. Result exposes failed stage and original subsystem result.
5. No public Inspector field or asset serialization break.
6. No actor/pool/pause integration is introduced in the same phase.
```
