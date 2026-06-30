# FXX-AUDIT — RuntimeContent / ContentAnchor Materialization Orchestration

Status: Draft / audit-only / documentation governance  
Scope: code-level audit of the prefab materialization path in `Packages/com.immersive.framework/Runtime/RuntimeContent` and `Packages/com.immersive.framework/Runtime/ContentAnchor`.

This document does not implement or authorize code changes. It exists to decide whether a follow-up ADR and implementation plan should be accepted.

---

## 1. Executive summary

The prefab-at-anchor path is functionally coherent, but it is currently too expensive to understand, reuse and extend.

The current path proves the right architectural boundaries:

```text
RuntimeContent state/ownership
Unity physical materialization
ContentAnchor logical binding
Unity physical placement
Runtime release / rollback
```

The issue is not that these responsibilities exist. They should exist. The issue is that their orchestration is currently concentrated in a Unity-facing bridge/pipeline pair instead of a reusable runtime-level orchestration service.

Observed in the current package snapshot:

```text
Runtime/ContentAnchor/UnityContentAnchorMaterializationBridge.cs      ~738 lines
Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs    ~364 lines
Runtime/RuntimeContent/RuntimeContentRuntime.cs                       ~742 lines
Runtime/ContentAnchor/UnityContentAnchorPlacementAdapter.cs           ~158 lines
```

The final physical placement side effect is small:

```csharp
instanceTransform.SetParent(anchorTransform, false);
```

But reaching that side effect requires a chain of runtime context resolution, declaration/set construction, request construction, adapter construction, materialization, handle state transition, evidence validation, logical anchor binding, physical placement and rollback.

That complexity is not inherently wrong for a lifecycle framework. It becomes a problem because there is no reusable orchestration layer that explains and owns the whole operation.

---

## 2. Path traced

Starting point:

```text
UnityContentAnchorMaterializationBridge.MaterializePrefabAtAnchor()
```

Core sequence:

```text
UnityContentAnchorMaterializationBridge
  -> resolves FrameworkRuntimeHost
  -> resolves/creates RuntimeScopeContext through RuntimeContentRuntime
  -> builds ContentAnchorDeclaration
  -> builds ContentAnchorSet
  -> builds RuntimeMaterializationResource
  -> builds ContentAnchorBindingRequest
  -> constructs UnityPrefabRuntimeMaterializationAdapter
  -> constructs UnityObjectRuntimeReleaseAdapter
  -> constructs UnityContentAnchorPlacementAdapter
  -> constructs UnityContentAnchorMaterializationPipeline
  -> calls MaterializeBindPlace(...)
```

Pipeline sequence:

```text
1. TryCreateMaterializationRequest(...)
2. UnityPrefabRuntimeMaterializationAdapter.Materialize(...)
3. RuntimeContentRuntime.ApplyMaterializationResult(...)
4. UnityRuntimeMaterializedObjectRegistry evidence check
5. FrameworkRuntimeHost.BindContentAnchor(...)
6. UnityContentAnchorPlacementAdapter.Place(...)
```

Failure compensation currently calls rollback from several failure points.

---

## 3. Findings

### 3.1. The Bridge is too thick

`UnityContentAnchorMaterializationBridge` is a MonoBehaviour-facing authoring bridge, but it currently does more than read Inspector state and submit a command. It builds runtime inputs, creates adapters and directly owns the composition of the materialization pipeline.

Expected bridge role:

```text
Read Inspector fields -> validate authoring -> call a reusable service -> expose QA diagnostics.
```

Current bridge role:

```text
Read Inspector fields -> build declarations/sets/requests -> create adapters -> create pipeline -> execute orchestration -> map result -> expose QA diagnostics.
```

This makes the only practical way to reuse prefab-at-anchor materialization outside Inspector/scene authoring be copying or depending on a Unity bridge.

### 3.2. The orchestration exists, but at the wrong layer

`UnityContentAnchorMaterializationPipeline` is the closest thing to the missing orchestration layer, but it is internal, Unity-specific and constructed by the bridge. That makes it hard to reuse from future consumers such as:

```text
- procedural spawners;
- actor materialization;
- pooled content materialization;
- pause/loading/transition surfaces;
- QA scenario runners that should not depend on a scene bridge.
```

The reusable unit should be a service/coordinator, not a MonoBehaviour bridge.

### 3.3. Rollback is explicit but not yet modeled as a reusable operation pattern

The pipeline has a central `RollbackPhysicalAndLogical(...)` helper, which is good. However, the main method still coordinates rollback manually from each failure point.

This is a candidate for a common operation pattern:

```text
forward step -> compensation step -> aggregate result
```

The project should not immediately introduce a generic Saga framework unless a second real use case is confirmed. But the audit should mark this as a likely Common operation primitive.

### 3.4. Status remapping creates debugging friction

The current path has several status layers:

```text
RuntimeMaterializationStatus
ContentAnchorBindingStatus
UnityContentAnchorPlacementStatus
UnityContentAnchorMaterializationPipelineStatus
UnityContentAnchorMaterializationBridgeStatus
```

The status layering is understandable for boundaries, but the aggregate result should expose:

```text
failed stage + original stage result + rollback state
```

rather than forcing every layer to reinterpret the failure into a new semantic enum.

### 3.5. RuntimeContentRuntime is too broad

`RuntimeContentRuntime` currently concentrates root management, context creation, handle declaration/registration, materialization request creation, materialization result application, release request creation, logical release and scope release.

This is too much surface for one runtime coordinator.

Candidate internal responsibilities:

```text
RuntimeScopeRootManager
RuntimeContentHandleRegistry
RuntimeMaterializationRequestFactory
RuntimeMaterializationStateApplier
RuntimeReleaseCoordinator
```

Do not split this class as the first implementation cut. It is heavily used and should be split only after the orchestration service proves its boundary.

---

## 4. What is good and should be preserved

- RuntimeContent and ContentAnchor are separated instead of hard-wired.
- Side effects are explicit and return result objects.
- Physical Unity materialization is adapter-based.
- Release/rollback exists; failures are not ignored.
- ContentAnchor binding is logical before being treated as visual placement.
- The bridge is explicitly marked experimental, which gives room to correct the shape.

---

## 5. Risk assessment

| Risk | Severity | Reason |
|---|---:|---|
| Reuse blocked behind MonoBehaviour bridge | High | Future systems may copy the bridge/pipeline path instead of sharing one service. |
| Debugging requires decoding too many status layers | Medium/High | The original failure stage can be obscured by wrapper statuses. |
| RuntimeContentRuntime becomes a god class | High | It already owns too many lifecycle responsibilities. |
| Premature generic Saga framework | Medium | A generic compensation runner can over-engineer if introduced before a second use case. |
| Breaking QA materialization smoke | High | The current path likely has accepted smoke behavior that must remain stable. |

---

## 6. Recommendation

Accept a new ADR and plan for a conservative consolidation:

```text
MAT-A — Audit closeout and diagram only
MAT-B — Introduce ContentAnchorMaterializationService as a reusable orchestration boundary
MAT-C — Move Bridge to thin-wrapper role
MAT-D — Preserve current pipeline diagnostics but expose failed stage + original result
MAT-E — Evaluate, not implement by default, a compensating operation helper
MAT-F — Plan RuntimeContentRuntime responsibility split after service migration
```

Do not split `RuntimeContentRuntime` first. Do not change public authoring fields first. Do not convert this into a generic materialization framework for actors, pooling or pause in the same phase.

---

## 7. Non-goals

```text
No change to public Inspector fields.
No change to prefab placement behavior.
No change to ContentAnchor identity semantics.
No new lifecycle runtime.
No pooling integration.
No actor materialization integration.
No replacement of RuntimeContentRuntime in the first implementation cut.
No broad generic Saga framework unless validated by a second use case.
```

---

## 8. Validation expected

Before/after comparison should include:

```text
- existing materialization QA smoke output;
- prefab instantiated count;
- registry active count;
- RuntimeContent handle count;
- ContentAnchor binding count;
- rollback behavior for at least one forced failure case if an existing smoke already covers it.
```

If the output changes, the implementation should be treated as behavior change and rejected unless explicitly documented as a bug fix.
