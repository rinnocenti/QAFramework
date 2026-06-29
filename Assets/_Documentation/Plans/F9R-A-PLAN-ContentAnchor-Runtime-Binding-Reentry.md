# F9R-A — ContentAnchor Runtime Binding Re-entry

Status: Draft / Plan

Type: Audit / Plan / ADR / docs-only.

This cut does not implement runtime behavior, physical placement, a concrete materializer, a concrete release adapter or any consumer. It does not select implementation.

F9R-A re-enters ContentAnchor runtime binding after the accepted F8R-B, F8R-C and F8R-D boundaries. The goal is to define what logical ContentAnchor binding may own before any future adapter places, parents, instantiates, destroys, pools or releases physical Unity content.

## Accepted Baseline

`F8R-A - RuntimeContent / ContentAnchor Materialization Audit` records the current ContentAnchor state:

| Area | Current state | Evidence |
|---|---|---|
| ContentAnchor identity / vocabulary | Exists as explicit anchor identity and authoring vocabulary. | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorId.cs`, `ContentAnchorScope.cs`, `ContentAnchorKind.cs`, `ContentAnchorRequiredness.cs`, `ContentAnchorDeclaration.cs` |
| RouteContentAnchor authoring | Exists as passive Route-scoped authoring evidence. | `Packages/com.immersive.framework/Runtime/ContentAnchor/RouteContentAnchor.cs` |
| ActivityContentAnchor authoring | Exists as passive Activity-scoped authoring evidence. | `Packages/com.immersive.framework/Runtime/ContentAnchor/ActivityContentAnchor.cs` |
| Discovery | Exists for already-loaded Route scenes and Activity discovery scopes. | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDiscoveryRuntime.cs`, `ContentAnchorDiscoveryResult.cs`, `ActivityContentAnchorDiscoveryResult.cs` |
| ContentAnchorSet | Exists as a passive immutable declaration set with local issue reporting. | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorSet.cs`, `ContentAnchorSetIssue.cs`, `ContentAnchorSetIssueKind.cs` |
| RuntimeContentAnchorBinding | Exists as logical/experimental binding correlation. | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs` |
| ContentAnchorContentHandle | Exists as passive correlation between an authored anchor and a logical runtime content handle. | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorContentHandle.cs` |

F8R-A also records what is still absent:

- no physical placement;
- no transform parenting;
- no prefab attach;
- no overlay root placement;
- no physical lifecycle cleanup.

F8R-B/B1 accepts `RuntimeScopeRoot`, `RuntimeContentHandle` and `RuntimeReleasePolicy` as logical RuntimeContent core language only.

F8R-C/C1 accepts the future Runtime Materialization Adapter as the first physical materialization boundary.

F8R-D/D1 accepts the future Physical Release Adapter as the physical cleanup boundary.

## Boundary Map

| Boundary | Allowed responsibility | Not allowed |
|---|---|---|
| ContentAnchor declaration / authoring | Declare explicit anchor id, owner, scope, kind, requiredness and diagnostic labels. | Register runtime services, move content, instantiate prefabs, create fallback identities or block lifecycle by itself. |
| ContentAnchor discovery | Inspect already-loaded Route/Activity scenes for authored anchors and produce `ContentAnchorSet` evidence. | Create anchors, load scenes, materialize content, place content or resolve consumer-specific behavior. |
| Logical binding request/result | Validate explicit anchor/runtime request fields, resolve a declaration from a supplied set and report canonical status. | Create `GameObject`, move `Transform`, parent hierarchy objects, call `Instantiate`, call `Destroy`, load Addressables or return pools. |
| Logical ContentAnchor content handle | Correlate one anchor declaration to one logical `RuntimeContentHandle`. | Represent a Unity object, physical placement, pooled lease, Addressables handle, scene object or cleanup authority. |
| Future physical placement adapter | May place physical adapter-owned content at a resolved anchor after a later accepted cut. | Live inside pure ContentAnchor core or run before explicit acceptance/implementation. |
| Future Runtime Materialization Adapter | May create/obtain physical content after accepted implementation. | Leak Unity object references into pure RuntimeContent or ContentAnchor logical handles. |
| Future Physical Release Adapter | May clean physical content after accepted implementation and ownership proof. | Treat logical binding cleanup as `Destroy`, pool return, Addressables release or scene unload. |

## Logical Binding Responsibilities

ContentAnchor logical binding may:

- validate anchor id, anchor scope and anchor kind;
- validate runtime owner, runtime scope and runtime content identity;
- validate requiredness as diagnostic authoring policy;
- produce `ContentAnchorBindingRequest` and `ContentAnchorBindingResult`;
- associate logical `RuntimeContentIdentity` / `RuntimeContentHandle` with a logical ContentAnchor declaration;
- register diagnostics through source, reason, status and diagnostic strings;
- clean logical bindings during release, Route scope exit or Activity scope exit.

ContentAnchor logical binding must not:

- create `GameObject`;
- move `Transform`;
- parent objects in hierarchy;
- instantiate prefab;
- destroy object;
- load Addressables;
- return pool;
- resolve camera, audio or gameplay placement;
- fabricate identity from Unity name, hierarchy path, scene path or asset path.

## Binding Rules

| Case | Rule | Evidence / baseline |
|---|---|---|
| Missing anchor | Return an explicit missing-anchor result. Do not create a fallback anchor. | `ContentAnchorBindingResult.MissingAnchor`, `ContentAnchorBindingStatus.FailedMissingAnchor` |
| Duplicate anchor | Treat duplicate declaration identity / duplicate anchor id as `ContentAnchorSet` diagnostic issues. Do not choose one by hierarchy order as fallback. | `ContentAnchorSet.cs`, `ContentAnchorSetIssueKind.cs` |
| Scope mismatch | Reject request/declaration mismatches through explicit scope/owner/kind identity checks. | `ContentAnchorBindingRequest.Matches`, `ContentAnchorDeclaration.GetOwnerDomain` |
| Route / Activity mismatch | Discovery may skip anchors whose authored owner does not match the active Route or Activity. | `ContentAnchorDiscoveryRuntime.cs`, `RouteContentAnchor.MatchesRoute`, `ActivityContentAnchor.MatchesActivity` |
| Stale content handle | Released runtime handles cannot be bound and stale bindings become invalid. | `RuntimeContentAnchorBinding.Bind`, `ContentAnchorContentHandle.IsValid` |
| Already bound content | Return `SucceededAlreadyBound` only when the existing binding handle remains valid and matches the registered runtime handle. | `RuntimeContentAnchorBinding.EvaluateExistingBinding`, `ContentAnchorBindingStatus.SucceededAlreadyBound` |
| Binding cleanup | Cleanup removes logical binding registry entries only. It must not move, destroy, release or unload physical objects. | `ContentAnchorBindingLifecycleResult.cs`, `RuntimeContentAnchorBinding.Unbind*` methods |
| Required vs optional anchor | Requiredness is authoring/diagnostic policy until a later accepted lifecycle gate changes it. It does not authorize physical placement. | `ContentAnchorRequiredness.cs`, `RouteContentAnchor.cs`, `ActivityContentAnchor.cs` |
| Diagnostics identity | Unity object name, hierarchy path, scene path and asset path may be diagnostic text only, not functional identity. | `RouteContentAnchor.cs`, `ActivityContentAnchor.cs`, `ContentAnchorDeclaration.cs` |

## Consumer Implications

| Consumer | F9R-A implication |
|---|---|
| Pause content | Remains blocked for physical overlay/content placement until a physical placement adapter is accepted and implemented. |
| Camera | Remains blocked from anchor-based camera placement or rigs until physical placement ownership is accepted. |
| Audio | Remains blocked from emitter placement or lifecycle cleanup until physical placement and release adapters are accepted. |
| Actor materialization | Remains blocked; logical binding is not actor spawn, movement or player prefab ownership. |
| Pooling/runtime-spawned | Remains blocked until pooling/materialization/release ownership is accepted in an adapter cut. |
| Save/progression | Remains blocked from physical object lifetime assumptions; logical identity may be cited only as logical state. |
| Gameplay | Remains blocked. F9R-A does not authorize F34, gameplay command reading or gameplay object placement. |

## Open Questions

| Question | Why it remains open | Candidate owner |
|---|---|---|
| Should required anchors block lifecycle, materialization requests or only adapter execution? | Current requiredness is diagnostic authoring policy only. | F9R-B or later lifecycle gate plan |
| Where should physical placement evidence live? | Pure ContentAnchor handles cannot expose `GameObject` or `Transform`. | F9R-B |
| Should placement happen before or after logical binding succeeds? | Current binding only correlates a registered runtime handle to a declaration. | F9R-B / future implementation readiness review |
| How should physical release coordinate with binding cleanup? | Current cleanup removes logical entries only; F8R-D keeps physical release in adapters. | F8R-F or future release implementation plan |
| Should Route and Activity anchor binding have separate adapter policies? | Discovery scopes and ownership differ, but physical placement is not accepted. | F9R-B |

## Candidate Cuts After Approval

These are candidates only. F9R-A does not select implementation automatically.

| Candidate | Type | Purpose | Selection state |
|---|---|---|---|
| F9R-A1 — ContentAnchor Runtime Binding Re-entry Acceptance | Docs-only / ADR acceptance | Accept or revise this logical binding re-entry boundary. | Not selected automatically. |
| F9R-B — ContentAnchor Physical Placement Adapter Plan | Plan / ADR candidate | Define where physical placement adapter behavior may live and how it relates to materialization/release adapters. | Not selected automatically. |
| F8R-F — Materialization/Release Implementation Readiness Review | Audit / readiness review | Check whether F8R-B/C/D and F9R-A/B are sufficient before any implementation cut. | Not selected automatically. |
| Future implementation | Implementation candidate | Implement only after explicit user acceptance of the required adapter boundary. | Not selected automatically. |

## Decision Point

Decide whether F9R-A is accepted as the logical ContentAnchor binding re-entry baseline.

If accepted, the next non-code decision can be `F9R-A1 — ContentAnchor Runtime Binding Re-entry Acceptance` or a later `F9R-B — ContentAnchor Physical Placement Adapter Plan`. No implementation is selected by this draft.
