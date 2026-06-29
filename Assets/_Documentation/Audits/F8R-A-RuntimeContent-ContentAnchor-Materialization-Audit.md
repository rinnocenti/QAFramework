# F8R-A - RuntimeContent / ContentAnchor Materialization Audit

Status: Draft / audit-only

Scope: documentation audit. No runtime, scene, prefab, asmdef or code changes are authorized by this file.

This audit is the first technical candidate after `POST-F33-B - Officialize/Reclassify F28-F33`, but it does not select a new implementation phase.

## 1. Executive Summary

`RuntimeContent` and `ContentAnchor` are present in `Packages/com.immersive.framework`, but the current package state is a logical/experimental runtime layer, not a complete physical materialization lane.

The package already has typed runtime content identity, owners, scopes, logical roots, a runtime registry, materialization request/result contracts, release request/result/policy contracts, route/activity scope creation, and logical Content Anchor binding. Evidence: `Packages/com.immersive.framework/Runtime/RuntimeContent`, `Packages/com.immersive.framework/Runtime/ContentAnchor`, `Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`, `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`, `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs`.

The package does not yet have a concrete physical prefab/materialization adapter, Unity hierarchy placement, automatic player/actor spawn, pooling rent/return, Addressables load/release, `Destroy`, `Instantiate`, or consumer-safe materialization ownership. Evidence: `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`, `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs`, `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`, `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`.

Therefore F8/F9 blockers are partially advanced but not closed. Camera, audio, save/progression, pooling/runtime-spawned, actor materialization and gameplay consumers remain blocked until a later accepted plan defines physical materialization, release and Content Anchor placement semantics.

## 2. Source Documents Read

| Source | Relevance |
|---|---|
| `Assets/_Documentation/Notes/Capability-Traceability-Matrix.md` | Matrix baseline: F8 owns runtime roots, runtime content handles, materialization request/result, materializer and release policy; F9 owns Content Anchor binding/runtime content handle/lifecycle placement. |
| `Assets/_Documentation/Notes/Package-System-XRay-Consolidated.md` | Historical package XRay. Useful as risk history, but stale where it predates current `RuntimeContent` and `ContentAnchor` runtime contracts. |
| `Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md` | Accepted closeout: F33 closed, no F34/gameplay selected, F8/F9 must be re-audited before consumers. |
| `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md` | Accepted reclassification: F28-F33 official only for the Input/Pause/Unity `PlayerInput` axis; F8R-A is the first technical candidate. |
| `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md` | Current governance plan: F8R-A is audit-only and does not select implementation. |
| `Packages/com.immersive.framework/Documentation~/README.md` | Canonical package documentation index and current phase map. |
| `Packages/com.immersive.framework/Documentation~/ADRs/ADR-INDEX.md` | Canonical ADR navigation and accepted matrix phase references. |

## 3. RuntimeContent State

| Capability | Real status | Evidence | QA / smoke evidence | Blocker | Recommendation |
|---|---|---|---|---|---|
| Runtime content identity | Present / experimental | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentIdentity.cs`, `RuntimeContentOwner.cs`, `RuntimeContentScope.cs`, `RuntimeContentId.cs` | Exercised indirectly by `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` in `RunRuntimeContentSmokeCore` and Content Anchor binding smoke helpers. | Identity exists, but consumer ownership rules for spawned objects are not accepted as a later implementation phase. | Keep as current logical baseline; do not expose new consumers until release/materialization ownership is planned. |
| Runtime content handle | Present / experimental / passive | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs`, `RuntimeContentState.cs`, `RuntimeContentHandleTransitionResult.cs`, `RuntimeContentHandleTransitionStatus.cs` | `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` validates declared/materialized/release state transitions in `RunRuntimeContentSmokeCore`. | Handle state is logical; it does not own a Unity object, pooled instance, scene object or Addressables handle. | Treat as F8 logical handle language, not physical content proof. |
| RuntimeContentRuntime owner | Present / experimental / internal | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs`; hosted by `Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs` | `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` calls `runtimeHost.RuntimeContentRuntime` in runtime content and binding smokes. | Runtime owner can declare/register/release logically, but there is no accepted physical materializer implementation. | Use as audit baseline for F8R-B planning. |
| Runtime scope roots | Present / experimental / logical | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`, `RuntimeRootRegistry.cs`, `RuntimeRootRegistryOperationResult.cs`, `RuntimeRootRegistryOperationStatus.cs` | `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` creates/removes transient roots in `RunRuntimeContentSmokeCore`. | `RuntimeScopeRoot` explicitly has no `GameObject`, `Transform`, `Instantiate`, `Destroy` or Content Anchor binding behavior. | Keep logical roots; define a separate physical root/placement policy before materializers. |
| Route runtime scope creation | Present / logical lifecycle integration | `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs` creates route owners and calls `CreateScopeRoot`. | Route Content Anchor binding cleanup smoke paths are in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Route scope creation does not imply physical materialization or placement. | Keep as integration evidence; require release/placement audit before consumers. |
| Activity runtime scope creation | Present / logical lifecycle integration | `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs` creates activity owners and calls `CreateScopeRoot`. | Activity Content Anchor binding smoke paths are in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Activity scope creation does not imply actor/prefab materialization. | Keep as integration evidence; do not advance actor materialization from this alone. |

## 4. ContentAnchor State

| Capability | Real status | Evidence | QA / smoke evidence | Blocker | Recommendation |
|---|---|---|---|---|---|
| Anchor identity and vocabulary | Present / experimental | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorId.cs`, `ContentAnchorScope.cs`, `ContentAnchorKind.cs`, `ContentAnchorRequiredness.cs`, `ContentAnchorDeclaration.cs` | Used by diagnostics and binding smokes in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Vocabulary exists, but placement and lifecycle semantics remain logical. | Keep as F7/F9 vocabulary baseline. |
| Anchor root/slot/point primitives | Present / passive | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorRoot.cs`, `ContentAnchorSlot.cs`, `ContentAnchorPoint.cs` | Route and Activity discovery smokes in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | These primitives do not instantiate, move, bind, spawn or resolve consumers. | Keep passive authoring language; do not treat as runtime placement. |
| Route Content Anchor authoring | Present / Unity authoring / passive side effect only as component presence | `Packages/com.immersive.framework/Runtime/ContentAnchor/RouteContentAnchor.cs` | `RunContentAnchorDiagnosticsSmoke`, `RunContentAnchorBindingSmoke` and `RunContentAnchorBindingCleanupSmoke` in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Component declaration does not register global services or materialize content. | Official as authoring evidence; not sufficient for physical binding. |
| Activity Content Anchor authoring | Present / Unity authoring / passive side effect only as component presence | `Packages/com.immersive.framework/Runtime/ContentAnchor/ActivityContentAnchor.cs` | `RunActivityContentAnchorBindingSmoke` in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Required Activity anchors are diagnostic; they do not block lifecycle by themselves. | Keep as Activity anchor evidence; define lifecycle policy before consumers. |
| Anchor discovery | Present / internal diagnostic runtime | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDiscoveryRuntime.cs`, `ContentAnchorDiscoveryResult.cs`, `ActivityContentAnchorDiscoveryResult.cs` | Discovery smokes in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Discovery does not create runtime roots, bind content physically, or materialize content. | Keep as discovery baseline; pair with F9R-A binding plan later. |
| Anchor set | Present / immutable logical set | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorSet.cs` | Binding smokes require a non-empty `ContentAnchorSet` in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Set de-duplicates and reports issues; it does not discover scene objects or execute lifecycle. | Keep as request input; do not promote to execution owner. |
| Runtime Content Anchor binding | Present / experimental / logical binding | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`, `ContentAnchorBindingRequest.cs`, `ContentAnchorBindingResult.cs`, `ContentAnchorContentHandle.cs` | `RunContentAnchorBindingSmoke`, `RunContentAnchorBindingCleanupSmoke`, `RunActivityContentAnchorBindingSmoke` in `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs`. | Binding does not move transforms, instantiate prefabs, unload scenes, perform physical release, or create fallback anchors. | Treat as F9 logical binding proof only; physical placement remains blocked. |

## 5. Runtime Root / Handle / Release Policy State

| Area | Real status | Evidence | Side effect Unity real? | Blocker | Recommendation |
|---|---|---|---|---|---|
| Runtime root registry | Present / internal / logical | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRootRegistry.cs` | No. The registry stores logical roots only. | No Unity hierarchy root ownership exists. | Plan physical root/placement separately if materializers require transforms. |
| Scope transition guard | Present / logical request freshness guard | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeTransitionGuard.cs`, `RuntimeScopeTransitionGuardResult.cs`, `RuntimeScopeTransitionGuardStatus.cs`, `RuntimeScopeTransitionState.cs` | No. | Guards materialization request freshness, not object lifetime. | Keep for request correctness; do not count as cleanup. |
| Materialization request/resource/result | Present / contract only | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationRequest.cs`, `RuntimeMaterializationResource.cs`, `RuntimeMaterializationResult.cs`, `RuntimeMaterializationStatus.cs` | No. | No concrete adapter produces Unity objects from resources. | Use as input to a future materializer ADR/plan. |
| Materialization adapter boundary | Present / interface only | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs` | No. The file explicitly excludes prefab, scene, Addressables, pooling and UnityEngine implementation from core. | No `PrefabContentMaterializer` or Unity adapter implementation is accepted in this cut. | Keep core pure; future implementation must live in the proper Unity adapter boundary. |
| Release request/result/policy | Present / logical release | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`, `RuntimeReleaseResult.cs`, `RuntimeReleasePolicy.cs`, `RuntimeReleaseStatus.cs` | No physical Unity side effect. | Release policy controls handle state/registry cleanup only; no destroy, scene unload, pool return or Addressables release. | Treat as logical F8 baseline; define physical release policy before consumers. |
| Release adapter boundary | Present / interface only | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs` | No. | No concrete physical release adapter exists. | Plan adapter implementation only after ownership decision. |

## 6. Materialization State

| Matrix capability | Real package status | Evidence | Current classification | Risk |
|---|---|---|---|---|
| Runtime scope root | Exists as logical root | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs` | Partial | Consumers may assume a Unity hierarchy root that does not exist. |
| Runtime root registry | Exists as internal logical registry | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRootRegistry.cs` | Partial | Lifecycle registration can be mistaken for physical ownership. |
| Runtime content handle | Exists as passive state handle | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs` | Partial | Handle does not own a spawned object or release handle. |
| Materialization request/result | Exists as pure contract | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationRequest.cs`, `RuntimeMaterializationResult.cs` | Partial | Current smokes synthesize results; they do not prove actual materialization. |
| Prefab content materializer | Not present | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs` defines the boundary only; no concrete materializer file exists under `Packages/com.immersive.framework/Runtime/RuntimeContent`. | Absent | Camera/audio/gameplay/actor consumers would need real objects and ownership. |
| Runtime release policy | Exists as logical policy | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs` | Partial | No physical cleanup means resource leaks are unresolved for future objects. |
| Destroy policy | Not physically implemented | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`, `RuntimeReleaseResult.cs` explicitly describe no destroy/unload/pool/Addressables side effect. | Absent / logical only | Consumers could leak or double-release objects if added prematurely. |
| Content Anchor binding request/result | Exists as logical request/result | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingRequest.cs`, `ContentAnchorBindingResult.cs` | Partial | Request/result do not place content. |
| Runtime Content Anchor binding | Exists as logical registry | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs` | Partial | Binding count is not physical attachment or transform placement. |
| Overlay/content root placement model | Not physically implemented | `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorRoot.cs`, `ContentAnchorSlot.cs`, `ContentAnchorPoint.cs` are passive primitives. | Absent | Pause/content overlay consumers remain blocked. |

## 7. Consumer Blockers

| Consumer axis | Current blocker | Evidence | Do not do now |
|---|---|---|---|
| Gameplay / commands | No accepted materialized actor/content lane; F33 explicitly did not select gameplay. | `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`, `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs` | Do not create F34/gameplay. |
| Actor materialization | `PlayerActor` identity exists from F31, but runtime prefab/player spawn is excluded by F31-F33 and physical materialization is absent. | `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`, `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationResult.cs` | Do not spawn actors or wire `PlayerInputManager.JoinPlayer`. |
| Camera | Camera consumers need stable runtime content ownership and anchors; physical placement is not implemented. | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`, `RuntimeContent/RuntimeScopeRoot.cs` | Do not add camera adapters before F8/F9 plan. |
| Audio | Audio emitters require materialization/release and possibly pooling; release is logical only. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`, `IRuntimeReleaseAdapter.cs` | Do not add audio adapters before release semantics. |
| Save/progression | Saveable runtime content needs stable identity and lifecycle ownership; handle identity exists but physical object lifecycle does not. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentIdentity.cs`, `RuntimeContentHandle.cs`, `RuntimeReleaseResult.cs` | Do not add save/progression consumers that assume object lifetime. |
| Pooling/runtime-spawned | Pool rent/return is absent from current `RuntimeContent` core. | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`, `IRuntimeReleaseAdapter.cs` | Do not integrate pooling until F8/F11 ownership is explicit. |
| Pause content materialization | Pause can drive InputMode/PlayerInput through F32/F33, but content overlay materialization is still blocked by Content Anchor physical placement. | `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`, `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorContentHandle.cs` | Do not treat F33 Pause input as Pause content materialization. |

## 8. Conflicts With Existing Documentation

| Document / claim area | Conflict | Evidence | Resolution |
|---|---|---|---|
| `Assets/_Documentation/Notes/Package-System-XRay-Consolidated.md` | The XRay is stale where it describes no current `RuntimeContent`/`ContentAnchor` runtime layer. | Current files exist under `Packages/com.immersive.framework/Runtime/RuntimeContent` and `Packages/com.immersive.framework/Runtime/ContentAnchor`. | Treat XRay as historical risk context, not current implementation truth. |
| Matrix F8 materialization reading | Some F8 capabilities are present as logical contracts, but not as physical materializers/release adapters. | `Assets/_Documentation/Notes/Capability-Traceability-Matrix.md`, `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`, `RuntimeReleasePolicy.cs` | Reclassify F8 as partial/experimental until a concrete materialization plan is accepted. |
| Matrix F9 Content Anchor binding reading | Logical binding exists, but placement/lifecycle/content root behavior is not physically implemented. | `Assets/_Documentation/Notes/Capability-Traceability-Matrix.md`, `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`, `ContentAnchorPoint.cs` | Reclassify F9 as partial/experimental until F9R-A resolves physical binding semantics. |
| POST-F33 next-step reading | POST-F33-B says F8R-A is first technical candidate, not a new implementation phase. | `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`, `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md` | Keep F8R-A audit-only and do not update roadmap with a new implementation phase yet. |
| QA smoke interpretation | QA smokes validate specific logical cases only; they do not prove architecture complete. | `Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs` manually creates materialization results and logical bindings. | Cite smoke evidence narrowly. |

## 9. Candidate Follow-up Cuts

| Candidate | Type | Why it is candidate | Resolves | Unlocks | ADR before code |
|---|---|---|---|---|---|
| F8R-B - Runtime Root / Handle / Release Policy Plan | Plan / ADR | The audit found logical roots, handles and release policy, but no physical ownership decision. | Ownership semantics for logical roots, handles and release. | Later materializer implementation and safe lifecycle cleanup. | Yes. |
| F8R-C - Runtime Materialization Adapter Boundary Plan | Plan / ADR | The package has `IRuntimeMaterializationAdapter` but no concrete materializer lane. | Where prefab/materializer code may live and what it may own. | Concrete materialization implementation after approval. | Yes. |
| F9R-A - ContentAnchor Runtime Binding Re-entry | Audit / plan | Logical binding exists, but physical placement and lifecycle cleanup need accepted semantics. | Anchor placement, binding lifecycle and cleanup boundaries. | Pause content materialization and later consumer attachment. | Yes before code. |
| F8R-D - Physical Release Adapter Plan | Plan | Current release is logical registry/handle cleanup only. | Destroy/pool/Addressables/scene release policy split. | Runtime-spawned, pooling and asset-backed content. | Yes. |
| F8R-E - Consumer Admission Gate | Audit / plan | Multiple consumers are blocked by the same unresolved materialization/release boundary. | Explicit criteria for allowing camera/audio/save/gameplay later. | Safer future consumer sequencing. | No code; ADR only if it changes roadmap rules. |

## 10. Recommended Decision Point

Decide whether this F8R-A audit is accepted as the current state baseline.

If accepted, the next non-code decision should be whether to open `F8R-B - Runtime Root / Handle / Release Policy Plan` before any materialization implementation.

If not accepted, the user should identify which evidence row needs re-audit before any new implementation or roadmap update.
