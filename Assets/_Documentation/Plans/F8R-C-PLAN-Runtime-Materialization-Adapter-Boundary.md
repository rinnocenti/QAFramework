# F8R-C — Runtime Materialization Adapter Boundary Plan

Status: Accepted / Plan

Type: Docs-only / boundary planning.

This plan does not implement a physical materializer. It does not select implementation. It only defines the boundary that must be accepted before any runtime code can instantiate, obtain, place, release or expose physical Unity content for RuntimeContent.

## Acceptance Closeout

F8R-C is accepted as the planning baseline for the Runtime Materialization Adapter boundary.

This accepts the adapter boundary only. It does not select implementation and does not authorize a physical materializer, prefab materializer, Addressables adapter, pooling adapter, physical release adapter, ContentAnchor physical placement, actor spawn, gameplay, camera, audio, save/progression, pooling/runtime-spawned work or F34.

`F8R-D — Physical Release Adapter Plan`, `F9R-A — ContentAnchor Runtime Binding Re-entry` and any future materializer implementation remain candidates. None of them is selected automatically by this acceptance.

## Baseline

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` records that RuntimeContent and ContentAnchor already have logical contracts for typed runtime content identity, owners, scopes, roots, handles, materialization request/result, release request/result/policy and logical ContentAnchor binding.

Relevant current package evidence:

- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationRequest.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationResource.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationResult.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationStatus.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRootRegistry.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingRequest.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`

`F8R-B — Runtime Root / Handle / Release Policy Plan` and `F8R-B1 — Runtime Root / Handle / Release Policy Acceptance` accept the central rule: RuntimeContent core keeps root, handle and release policy as logical framework language. `RuntimeScopeRoot` is not a `GameObject` or `Transform`. `RuntimeContentHandle` is not a Unity object, pooled instance, Addressables handle or placement. `RuntimeReleasePolicy` stays logical. Physical root, placement, instantiate, destroy, pool return, scene unload and Addressables release stay outside the pure core.

## Boundary Map

| Area | Allowed responsibility | Not allowed |
|---|---|---|
| Pure RuntimeContent core | Own `RuntimeMaterializationRequest`, `RuntimeMaterializationResource`, `RuntimeMaterializationResult`, `RuntimeMaterializationStatus`, typed identity, owner, scope, logical root, handle state, guards and diagnostics. | `GameObject`, `Transform`, `Object.Instantiate`, `Object.Destroy`, Addressables handles, pooled instance references, scene object ownership or ContentAnchor physical placement. |
| Future Unity materialization adapter | First authorized boundary for physical materialization after explicit acceptance and later implementation selection. It may interpret a request/resource and perform Unity-side physical work inside its own adapter layer. | Leaking Unity objects into pure core, inventing ownership, creating fallback roots, replacing request identity or making consumers depend on physical objects before implementation is accepted. |
| Future optional Addressables adapter | Optional adapter layer that may resolve Addressables resources after a separate accepted cut. | Addressables handles or release semantics in RuntimeContent core. |
| Future optional Pooling adapter | Optional adapter layer that may obtain and return pooled instances after a separate accepted cut. | Pooled instance references or pool-return ownership in RuntimeContent core. |
| Project assets/configs | Supply authored resource descriptors, prefab references or project-specific adapter configuration after an accepted implementation cut. | Becoming canonical framework identity, lifecycle ownership or hidden fallback materialization behavior. |

## Future Adapter Inputs

A future materialization adapter may receive:

- `RuntimeMaterializationRequest` produced by `RuntimeContentRuntime`.
- `RuntimeMaterializationResource` with explicit `ResourceType` and `ResourceKey`.
- Logical owner, scope and identity from the request context.
- Logical root context from the RuntimeContent root/registry language.
- Logical ContentAnchor binding context if a later `F9R-A` cut accepts re-entry for ContentAnchor runtime binding.

The adapter must treat `ResourceName` and `ResourcePath` as diagnostics only. They are not functional identity and must not be used as fallback ownership.

## Future Adapter Outputs

A future materialization adapter may return:

- `RuntimeMaterializationResult` with explicit `RuntimeMaterializationStatus`.
- A matching `RuntimeContentHandle` on success.
- Diagnostics that identify the adapter, resource type/key, owner, scope, content id, reason and failure mode.
- A logical handle update request/result flow if a later accepted cut separates adapter execution from core handle registration.
- Physical object evidence only inside the adapter boundary. The pure core must not store or expose Unity object references.

Consumers must not infer a `GameObject`, `Transform`, prefab instance, pooled instance, Addressables handle or physical placement from `RuntimeContentHandle`.

## Forbidden In Core

RuntimeContent core must not contain or depend on:

- `GameObject`
- `Transform`
- `Object.Instantiate`
- `Object.Destroy`
- Addressables handles
- pooled instance references
- direct scene object ownership
- ContentAnchor physical placement

## Failure Model

| Failure | Required treatment |
|---|---|
| Missing resource | Return a failed materialization result with diagnostics identifying the missing `RuntimeMaterializationResource`; do not fall back to Unity names or paths. |
| Missing adapter | Fail before physical work; do not create a default Unity materializer in core. |
| Invalid owner | Reject before adapter execution or during result application using typed owner/scope diagnostics. |
| Stale scope | Reject as stale or cancelled scope; do not materialize into an old Route/Activity/Session context. |
| Duplicate materialization | Reject or return an already-owned logical handle only through explicit core registry rules; do not create a second physical instance silently. |
| Adapter exception/failure | Convert to explicit failed result and diagnostics; no silent fallback. |
| Unsupported resource type | Fail with explicit resource type diagnostics; do not infer prefab, Addressables or pool behavior from strings. |

Current status vocabulary evidence is in `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationStatus.cs`, including `FailedInvalidRequest`, `FailedMissingRoot`, `FailedInvalidHandle`, `RejectedMismatchedIdentity`, `FailedRegistration`, `FailedMaterializer`, `RejectedScopeTransition`, `RejectedScopeCancellation` and `RejectedStaleScope`.

## Diagnostics

- Unity names, scene paths, hierarchy paths and asset paths may be diagnostic text only.
- Functional identity must come from explicit ids, owners, scopes and resource keys.
- Diagnostics must report the adapter boundary that produced the result.
- Diagnostics must distinguish request creation, adapter execution, handle registration and later release.
- Diagnostics must not imply physical placement when only logical materialization has occurred.

## Non-Goals

- no implementation
- no prefab materializer
- no Addressables adapter
- no pooling adapter
- no physical release adapter
- no actor spawn
- no camera/audio/save/gameplay consumer
- no ContentAnchor physical placement

## Open Questions

- Should the first physical adapter be a narrow Unity prefab adapter or a more abstract Unity object provider?
- Should physical object evidence be adapter-local only, or should a later Unity-facing query API expose it without touching pure core?
- Should materialization result application stay synchronous in the core, or should a later cut introduce a separate logical handle update request/result?
- Should missing adapter be represented by an existing `RuntimeMaterializationStatus` or a new explicit status in a later code cut?
- Should ContentAnchor binding re-entry happen before or after the first physical materialization implementation?

## Candidate Cuts After Approval

| Candidate | Type | Purpose | Selection state |
|---|---|---|---|
| F8R-D — Physical Release Adapter Plan | Plan / ADR candidate | Define the future destroy/pool return/Addressables release boundary outside core. | Not selected automatically. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Audit / plan candidate | Re-enter logical ContentAnchor binding after materialization boundaries are explicit. | Not selected automatically. |
| Future implementation cut | Implementation candidate | Implement a physical materialization adapter only after explicit acceptance. | Not selected automatically. |

F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain blocked until the user explicitly selects a later cut.
