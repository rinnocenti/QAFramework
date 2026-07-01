# FXX-CLOSEOUT - MAT-1 RuntimeContent / ContentAnchor Materialization Ownership Batch

Status: Closed / MAT-1 implemented
Date: 2026-06-30

## 1. Decision

MAT-1 is closed as a narrow mechanical cut.

The repeated physical release -> logical release sequence shared by the ContentAnchor materialization and lifecycle release paths was consolidated into one internal helper:

- `ContentAnchorReleaseExecution`

Route and Activity remain the semantic owners of their lifecycle decisions.
ContentAnchor remains the owner of the mechanical materialization / binding / release sequencing.

## 2. What was consolidated

The following repeated sequence was extracted from the call sites and kept behind one internal helper:

1. create or receive a `RuntimeReleaseRequest`;
2. execute the explicit physical release adapter;
3. apply the logical `RuntimeContentRuntime` release result;
4. carry the physical and logical results forward for diagnostics.

The helper is used by:

- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationScopeReleasePipeline.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorCompositeLifecycleReleaseExecutor.cs`

The materialization rollback path also uses the same helper, so the same release ordering is now shared across the explicit materialization and release flows.

## 3. Files changed

### Runtime code

- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorReleaseExecution.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationPipeline.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorMaterializationScopeReleasePipeline.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/UnityContentAnchorCompositeLifecycleReleaseExecutor.cs`

### Documentation status only

- `Assets/_Documentation/Plans/FXX-PLAN-RuntimeContent-ContentAnchor-Materialization-Orchestration.md`
- `Assets/_Documentation/Plans/FXX-PLAN-Architecture-Consolidation-Roadmap.md`

## 4. Boundary preserved

- No public API was added or changed.
- No enum values were changed.
- No `asmdef` or `package.json` was changed.
- No scene, prefab, asset, pooling, addressables, actor, camera, audio or save path was introduced.
- No new lifecycle layer was created.
- No fallback or service locator was added.

## 5. What was not changed

- `RouteLifecycleRuntime` remained the semantic owner for Route transition behavior.
- `ActivityFlowRuntime` remained the semantic owner for Activity transition behavior.
- Existing diagnostics strings and status vocabularies were preserved.
- `RuntimeContentRuntime` remained the owner of logical runtime-content state and release application.
- `ContentAnchor` did not absorb Route or Activity semantics.

## 6. Validation performed

Static review only.

I verified the affected call paths and the helper extraction by inspection and text search.

No Unity compile, import, playmode or smoke was run in this turn.

## 7. Manual validation checklist

Run the expected QA smokes after Unity import/compile:

- Standard Smoke
- Content Anchor Diagnostics Smoke
- Activity Content Anchor Diagnostics Smoke
- Composite Lifecycle Release Smoke
- Route Release Smoke
- Activity Content Execution Participant Source Smoke

Also verify:

- materialization rollback still reports the same physical/logical failure shape;
- ContentAnchor release cleanup counts remain unchanged;
- Route and Activity lifecycle outputs remain unchanged.

## 8. Remaining risk

The helper is intentionally mechanical, but it was not validated in Unity in this turn.
If a later cut wants to reduce Route/Activity binding cleanup duplication, that should be a separate helper with its own scope and validation.

## 9. SOLID impact

- SRP improved: the repeated release sequence now has one internal owner.
- OCP preserved: existing consumers still call the same public surface.
- LSP preserved: no contract shape changed.
- ISP preserved: no new broad interface was introduced.
- DIP preserved: the helper stays internal and is injected only through existing runtime collaborators.
