# FXX-PLAN — RuntimeContent / ContentAnchor Materialization Orchestration

Status: Closed / MAT-4
Scope: implementation plan for `FXX-ADR-CONSOLIDATION-002`.  
Prerequisite: ADR accepted.

This plan is intentionally conservative. It extracts a reusable orchestration boundary before attempting larger splits or consumer integrations.

---

## 1. Objective

Make “materialize prefab at ContentAnchor” a reusable framework operation instead of a path effectively owned by a MonoBehaviour bridge.

The target shape:

```text
UnityContentAnchorMaterializationBridge
  -> reads Inspector/authored fields
  -> builds or resolves explicit request inputs
  -> calls ContentAnchorMaterializationService
  -> reports result/diagnostics
```

The service owns:

```text
Runtime request -> Unity instantiate -> Runtime apply -> evidence check -> logical bind -> physical place -> rollback
```

---

## 2. Cut line

| Cut | Name | Type | Depends on |
|---|---|---|---|
| MAT-A | Materialization Flow Audit Closeout | Documentation | none |
| MAT-B | Service Shell + Request/Stage Result | Additive implementation | ADR accepted |
| MAT-C | Service Delegates to Existing Pipeline | Internal refactor | MAT-B |
| MAT-D | Bridge Thin Wrapper Migration | Internal refactor | MAT-C |
| MAT-E | Stage-Oriented Result Consolidation | Internal refactor | MAT-D |
| MAT-F | RuntimeContentRuntime Split Plan | Documentation / decision point | MAT-D or MAT-E |
| MAT-G | Closeout / Next Decision | Documentation | MAT-E/F |

Do not combine MAT-B through MAT-E in one cut. Side effects require small reversibility.

---

## 3. MAT-A — Materialization Flow Audit Closeout

### Included

```text
Document the current 6-step flow.
Document line count and direct classes involved.
Document current bridge/pipeline/runtime responsibilities.
Add a sequence diagram or text sequence to Documentation~/Planning or Assets/_Documentation/Audits.
```

### Excluded

```text
No code change.
No smoke expectation change.
```

### Acceptance

```text
A reviewer can understand the prefab-at-anchor flow without opening 15+ files.
```

---

## 4. MAT-B — Service Shell + Request/Stage Result

### Included

```text
Runtime/ContentAnchor/ContentAnchorMaterializationService.cs
Runtime/ContentAnchor/ContentAnchorMaterializationStage.cs
Runtime/ContentAnchor/ContentAnchorMaterializationServiceResult.cs
Runtime/ContentAnchor/ContentAnchorMaterializationServiceRequest.cs (only if it reduces argument sprawl)
```

The service can initially be a shell that validates inputs and returns “not executed” only in synthetic tests, or delegates immediately in MAT-C if that is simpler.

### Excluded

```text
No bridge migration yet.
No pipeline deletion.
No RuntimeContentRuntime split.
```

### Acceptance

```text
Code compiles.
New synthetic smoke/test can construct the service without a MonoBehaviour.
No existing materialization behavior changes.
```

---

## 5. MAT-C — Service Delegates to Existing Pipeline

### Included

```text
Move orchestration entry point to ContentAnchorMaterializationService.MaterializeBindPlace(...).
Service may internally construct or receive:
  - UnityPrefabRuntimeMaterializationAdapter
  - UnityObjectRuntimeReleaseAdapter
  - UnityContentAnchorPlacementAdapter
  - UnityContentAnchorMaterializationPipeline
```

This cut is allowed to keep `UnityContentAnchorMaterializationPipeline` as an internal implementation detail. The important change is the public/reusable entry point.

### Excluded

```text
No behavior change.
No status redesign yet.
No bridge serialization changes.
```

### Acceptance

```text
Existing bridge path still passes.
A non-MonoBehaviour caller can call the service path in a QA/smoke context.
```

---

## 6. MAT-D — Bridge Thin Wrapper Migration

### Included

```text
UnityContentAnchorMaterializationBridge delegates orchestration to ContentAnchorMaterializationService.
Bridge keeps existing serialized fields.
Bridge keeps existing public methods.
Bridge remains responsible for Inspector/authored input and diagnostics only.
```

### Excluded

```text
No field rename.
No component split.
No asset migration.
```

### Acceptance

```text
Prefab materialization smoke output remains the same.
Inspector serialized data remains valid.
Bridge line count and responsibilities are reduced.
```

---

## 7. MAT-E — Stage-Oriented Result Consolidation

### Included

```text
Expose failed stage in the aggregate materialization result.
Preserve original subsystem result objects.
Reduce redundant status remapping where safe.
Keep compatibility shims if existing bridge result/status is consumed by QA.
```

### Excluded

```text
No forced removal of all existing status enums.
No public QA output rewrite unless approved.
```

### Acceptance

```text
A failure report identifies:
  - failed stage;
  - original subsystem result/status/message;
  - rollback attempted/succeeded;
  - physical/logical release results if rollback ran.
```

---

## 8. MAT-F — RuntimeContentRuntime Split Plan

### Included

Create a follow-up plan, not implementation, for splitting `RuntimeContentRuntime`.

Candidate seams:

```text
RuntimeScopeRootManager
RuntimeContentHandleRegistry
RuntimeMaterializationRequestFactory
RuntimeMaterializationStateApplier
RuntimeReleaseCoordinator
```

### Excluded

```text
No split in this cut unless separately accepted.
No API signature churn.
```

### Acceptance

```text
The split plan identifies public/internal call sites and orders cuts from least risky to highest risk.
```

---

## 9. MAT-G — Closeout / Decision Point

### Included

```text
Document smoke evidence.
Document before/after bridge/pipeline/runtime responsibilities.
Document whether UnityContentAnchorMaterializationPipeline remains needed.
Decide whether CompensatingStepRunner is justified by a second use case.
Decide whether RuntimeContentRuntime split starts next or waits.
```

### Excluded

```text
No automatic migration to Actor, Pooling, Pause, Loading or Transition.
```

---

## 10. Non-goals for the whole plan

```text
No new gameplay feature.
No pooling.
No actor materialization.
No Pause/Loading/Transition surface consumer.
No hidden fallback.
No reflection.
No service locator.
No broad generic Saga framework in the first pass.
No public Inspector field changes.
No lifecycle runtime creation in setup.
```

---

## 11. Codex execution rule

Codex should run this as two independent tasks:

```text
Task 1: audit-only / no code changes / produce exact cut list and risk notes.
Task 2: after review, implement MAT-B only.
```

MAT-4 superseded the earlier task split by explicit implementation request. It extracted `ContentAnchorMaterializationService`, migrated the bridge to the service, kept the pipeline as a compatibility wrapper, and preserved existing bridge result diagnostics.
