# F8R-B — Runtime Root / Handle / Release Policy Plan

Status: Draft / Plan

Scope: docs-only / Plan / ADR. This cut does not implement runtime behavior, does not create a physical materializer and does not authorize a new implementation phase.

F8R-B is a prerequisite planning cut for future physical materialization. Its purpose is to define ownership semantics for runtime roots, runtime content handles and release policy before any adapter creates, places, destroys, pools or releases physical Unity objects.

## 1. F8R-A Baseline

F8R-A concluded that `RuntimeContent` and `ContentAnchor` exist as logical/experimental runtime layers, not as physical materialization.

Accepted baseline from `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`:

| Area | Current state | Evidence |
|---|---|---|
| Runtime identity / owner / scope | Present as typed logical primitives. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentIdentity.cs`, `RuntimeContentOwner.cs`, `RuntimeContentScope.cs`, `RuntimeContentId.cs` |
| Runtime scope root / registry | Present as internal logical root and registry. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`, `RuntimeRootRegistry.cs` |
| Runtime content handle | Present as passive state handle. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs`, `RuntimeContentState.cs` |
| Materialization request/result | Present as pure contracts. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationRequest.cs`, `RuntimeMaterializationResult.cs`, `RuntimeMaterializationResource.cs` |
| Materialization adapter boundary | Present as interface only. | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs` |
| Release request/result/policy | Present as logical release contracts. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`, `RuntimeReleaseResult.cs`, `RuntimeReleasePolicy.cs` |
| Release adapter boundary | Present as interface only. | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs` |
| Content Anchor binding | Present as logical binding correlation. | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`, `ContentAnchorBindingRequest.cs`, `ContentAnchorContentHandle.cs` |

F8R-A also concluded that there is no accepted physical prefab materializer, no Unity hierarchy placement, no automatic player/actor spawn, no pooling rent/return, no Addressables load/release, no `Instantiate`, no `Destroy` and no consumer-safe materialization ownership.

## 2. Proposed Decisions

| Topic | Proposed decision | Evidence / rationale |
|---|---|---|
| Logical runtime root vs future physical Unity root | `RuntimeScopeRoot` remains a logical ownership/registry boundary. A future Unity adapter may create or resolve a physical `GameObject` / `Transform` root, but that physical root is not part of RuntimeContent core. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs` explicitly has no `GameObject`, `Transform`, `Instantiate`, `Destroy` or Content Anchor binding behavior. |
| Route / Activity / Session ownership | Runtime ownership is scoped by `RuntimeContentOwner` using `Session`, `Route`, `Activity` or `Transient`. Route and Activity runtimes may create logical scope roots for their lifecycle owners; Session ownership remains a distinct owner domain. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentOwner.cs`, `Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs`, `Packages/com.immersive.framework/Runtime/ActivityFlow/ActivityFlowRuntime.cs` |
| Runtime content handle semantics | `RuntimeContentHandle` represents one logical runtime content identity and its release state. It does not own a Unity object, pooled instance, scene object, Addressables handle or Content Anchor placement. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs` |
| Handle lifecycle states | Keep current vocabulary: `Declared`, `Materialized`, `ReleaseRequested`, `Released`, `ReleaseFailed`. These are logical lifecycle states and must not imply physical Unity object state until an adapter maps them explicitly. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentState.cs` |
| Release request/result semantics | `RuntimeReleaseRequest` names a valid runtime scope context, identity and explicit release policy. `RuntimeReleaseResult` reports logical handle/registry outcome plus adapter failure diagnostics; it does not destroy objects, unload scenes, return pools or release Addressables handles. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`, `RuntimeReleaseResult.cs`, `RuntimeReleaseStatus.cs` |
| Release policy names and meaning | Keep current names: `MarkReleasedOnly` means keep the handle registered for diagnostics or later owner cleanup; `MarkReleasedAndUnregister` means mark released and remove the handle from the logical scope root. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs` |
| Stale handle behavior | Released handles must not be accepted for new Content Anchor bindings. Releasing an already released handle is idempotent at the logical layer; missing root/handle remains an explicit failure result. | `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`, `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseResult.cs`, `RuntimeReleaseStatus.cs` |
| Duplicate root behavior | Creating a root for an existing owner is not a fallback or replacement; it returns `RootAlreadyExists` and reopens/keeps the logical guard for that owner. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRootRegistry.cs`, `RuntimeContentRuntime.cs` |
| Duplicate handle behavior | A duplicate content id under the same owner is rejected unless the same handle is already registered. Runtime identity is `owner + contentId`, not a GameObject name or scene path. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`, `RuntimeContentIdentity.cs` |
| Owner identity requirements | Owner domain must match scope: Session -> `FrameworkIdentityDomain.Session`, Route -> `Route`, Activity -> `Activity`, Transient -> `Runtime`. Mismatched owner domains are invalid. | `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentOwner.cs` |
| Future physical destroy authority | A future Unity release adapter may destroy physical objects only when the accepted physical release plan says the runtime content is not pooled, not scene-owned, not Addressables-owned and is owned by the request identity. | F8R-A blocker list and `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs` |
| Core purity | RuntimeContent core must remain logical and must not depend on `UnityEngine`, prefab assets, scene hierarchy placement, pooling, Addressables or consumer modules. | `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`, `IRuntimeReleaseAdapter.cs` |

## 3. Ownership Model

Runtime ownership is defined by `RuntimeContentOwner`, not by physical objects.

| Scope | Owner source | Allowed now | Deferred |
|---|---|---|---|
| Session | Session identity / application session owner. | Logical root and handle ownership. | Physical persistent root, global bootstrap placement, player prefab spawn. |
| Route | Active Route identity. | Logical Route root creation and cleanup. | Physical Route content placement, route-owned prefab materialization. |
| Activity | Active Activity identity. | Logical Activity root creation and cleanup. | Actor materialization, Activity prefab placement, participant binding. |
| Transient | Runtime-only owner identity. | QA and explicit short-lived logical ownership. | General consumer runtime spawn semantics. |

The owner decides lifecycle and release authority. Consumers must not derive authority from `GameObject.name`, Transform hierarchy, scene path, action map, `PlayerInput`, camera rig or audio emitter.

## 4. Handle Lifecycle Semantics

| State | Meaning | Allowed transition source | Physical implication |
|---|---|---|---|
| `Declared` | Identity and owner are known; no concrete instance is proven by core. | Declaration or pre-materialization contract. | None. |
| `Materialized` | A materializer/result claims a concrete runtime instance exists. | Successful `RuntimeMaterializationResult` applied by runtime. | Adapter-defined only; core still holds no object reference. |
| `ReleaseRequested` | Release was requested for the identity. | `RuntimeReleaseRequest` / logical release flow. | Adapter-defined only. |
| `Released` | Logical release succeeded. | Logical release result after successful release flow. | Future adapter may already have cleaned physical resources before core marks released. |
| `ReleaseFailed` | Release failed. | Future adapter/core failure mapping. | Adapter-defined failure; core must report diagnostics. |

## 5. Release Policy Semantics

| Policy | Meaning | Use case | Not allowed to mean |
|---|---|---|---|
| `MarkReleasedOnly` | Mark the handle as `Released` and keep it registered. | Diagnostics, delayed owner cleanup, temporary audit trails. | Keep physical object alive by default. |
| `MarkReleasedAndUnregister` | Mark the handle as `Released` and remove it from the logical root. | Scope shutdown, cleanup smoke, root removal readiness. | Destroy a Unity object by itself. |

Physical cleanup must be a separate adapter concern. The core policy decides only logical state and registry retention.

## 6. Future Physical Adapter Constraints

A future Unity materialization adapter may:

- interpret a `RuntimeMaterializationRequest`;
- instantiate or otherwise obtain a physical object only after an accepted implementation cut;
- return a `RuntimeMaterializationResult` with a matching `RuntimeContentHandle`;
- use a future physical root/placement policy;
- fail fast when the request identity, owner, root or resource is invalid.

A future Unity release adapter may:

- receive a `RuntimeReleaseRequest`;
- perform physical cleanup only for content owned by the request identity;
- choose the cleanup mechanism only after a specific plan accepts `Destroy`, pool return, scene unload or Addressables release;
- return `RuntimeReleaseResult.AdapterFailure` or a canonical release result without mutating ownership identity.

Adapters must not:

- invent owner identity;
- create fallback roots silently;
- replace the request identity;
- bypass `RuntimeContentRuntime`;
- move Content Anchor placement semantics into RuntimeContent core;
- turn QA smoke paths into public materialization API.

## 7. Non-goals

- No `Instantiate`.
- No `Destroy`.
- No Addressables.
- No pooling.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No camera consumer.
- No audio consumer.
- No save/progression consumer.
- No gameplay consumer.
- No automatic ContentAnchor physical placement.
- No concrete materializer.
- No scene, prefab, asmdef or runtime code changes.
- No F34 selection.

## 8. Open Questions

| Question | Why it remains open | Proposed next owner |
|---|---|---|
| Should future physical roots be authored, created by adapter, or resolved from existing scene anchors? | `RuntimeScopeRoot` is logical; physical placement is not accepted. | `F8R-C` / `F9R-A` |
| Should physical release run before or after logical `Released` state is applied? | Current core applies logical release only; adapter orchestration is not implemented. | `F8R-D` |
| How should pooled content report ownership when the pool owns the instance storage but runtime owns the active lease? | Pooling is explicitly not selected yet. | Later F11/pooling-specific plan after F8R-D |
| Should `ReleaseFailed` be reachable by core-only flows or only by adapter failures? | Current logical release normally succeeds or returns explicit failure status without marking failed. | `F8R-D` |
| How should Content Anchor binding cleanup coordinate with physical release? | Current binding cleanup is logical; physical placement is absent. | `F9R-A` |

## 9. Candidate Implementation Cuts After Approval

These are candidates only. This plan does not select implementation.

| Candidate | Type | Purpose | Dependency |
|---|---|---|---|
| F8R-C — Runtime Materialization Adapter Boundary Plan | Plan / ADR | Define where physical materializer adapters live and how they consume `RuntimeMaterializationRequest/Result`. | F8R-B approval. |
| F8R-D — Physical Release Adapter Plan | Plan / ADR | Define physical cleanup policy split for destroy, pool return, scene unload and Addressables release. | F8R-B approval; F8R-C if materializer lifecycle is affected. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Audit / plan | Re-enter Content Anchor binding with accepted runtime handle/release ownership and decide physical placement semantics. | F8R-B approval; likely F8R-C/F8R-D decisions. |

## 10. Decision Point

Accept or revise the proposed F8R-B ownership semantics before any materialization adapter, physical release adapter or Content Anchor physical placement work begins.
