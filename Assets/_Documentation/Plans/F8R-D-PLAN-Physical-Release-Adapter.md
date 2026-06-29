# F8R-D — Physical Release Adapter Plan

Status: Accepted / Plan

Type: Docs-only / boundary planning.

This plan does not implement physical release. It does not select implementation. It defines the boundary that must be accepted before any runtime code can destroy, return to pool, release Addressables handles, unload scene-owned content or otherwise clean up physical Unity content for RuntimeContent.

## Acceptance Closeout

F8R-D is accepted as the planning baseline for the Physical Release Adapter boundary.

This accepts the physical release adapter boundary only. It does not select implementation and does not authorize a release adapter, materializer, `Destroy` call, pool return implementation, Addressables release implementation, scene unload implementation, ContentAnchor physical placement, actor spawn, gameplay, camera, audio, save/progression, pooling/runtime-spawned work or F34.

`F9R-A — ContentAnchor Runtime Binding Re-entry` and any future materializer/release adapter implementation remain candidates. None of them is selected automatically by this acceptance.

## Accepted Baseline

F8R-B and F8R-B1 accept that RuntimeContent core keeps root, handle and release policy as logical framework language. `RuntimeScopeRoot` is not a `GameObject` or `Transform`. `RuntimeContentHandle` is not a Unity object, pooled instance, Addressables handle or placement. `RuntimeReleasePolicy` remains logical.

F8R-C and F8R-C1 accept that a future Unity materialization adapter is the first authorized boundary for physical materialization. RuntimeContent core keeps request, result, identity, owner, scope, handle state, guards and diagnostics. Physical object evidence must stay inside adapter boundaries and must not leak into pure core.

Current package evidence:

- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseResult.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseStatus.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`

The current release runtime is logical. It marks handles released, optionally unregisters handles from logical roots and reports diagnostics. It does not destroy objects, unload scenes, return pools or release Addressables handles.

## Boundary Map

| Area | Allowed responsibility | Not allowed |
|---|---|---|
| Core logical release | Emit `RuntimeReleaseRequest`, carry `RuntimeReleasePolicy`, validate logical owner/scope/identity, apply logical handle state/registry effects and report diagnostics. | `Object.Destroy`, pool return, Addressables release, scene unload, physical object references, physical ownership inference or project cleanup policy execution. |
| Future Unity physical release adapter | Release Unity objects that were instantiated or otherwise owned by a future Unity materialization adapter after a later accepted implementation cut. | Inventing owner identity, destroying scene-owned objects, destroying pooled objects, releasing Addressables handles or leaking Unity object references into pure core. |
| Future Addressables release adapter | Release Addressables-owned resources that were loaded by an accepted Addressables materialization adapter. | Addressables handles in RuntimeContent core or implicit Addressables ownership from resource strings. |
| Future Pooling return adapter | Return active leases to a pool when the accepted pooling adapter owns the physical instance lifecycle. | Pooled instance references in RuntimeContent core or destroying pooled instances as default cleanup. |
| Future scene unload / scene-owned release boundary | Release scene-owned content or unload scenes only through a dedicated scene lifecycle boundary if a later plan accepts it. | Treating `RuntimeReleasePolicy` as permission to unload scenes or remove authored scene objects. |
| Project-owned cleanup policy | Declare project-specific cleanup choices in project configuration after an accepted implementation cut. | Becoming hidden framework fallback behavior or overriding typed owner/scope/identity checks. |

## Core Release Output

RuntimeContent core may emit or own:

- `RuntimeReleaseRequest`
- `RuntimeReleasePolicy`
- logical owner, scope and identity
- diagnostics

Core release may complete logical handle state and registry cleanup. It must not decide or execute physical cleanup mechanism.

## Future Adapter Execution

A future adapter may execute physical cleanup only after explicit acceptance and implementation:

- `Object.Destroy` for objects instantiated and owned by a future Unity materialization adapter.
- pool return for objects leased by a future Pooling adapter.
- Addressables release for objects/resources loaded by a future Addressables adapter.
- scene unload/release only in a dedicated scene-owned content boundary, if approved later.

The adapter must prove that the physical object/resource belongs to the release request identity and owner. It must not use Unity names, hierarchy paths, scene paths or asset paths as functional identity.

## Future Adapter Return

A future physical release adapter must return:

- status
- diagnostics
- physical release evidence
- logical release completion result, if applicable

Physical release evidence belongs to the adapter boundary. Pure RuntimeContent core must not store Unity object references, pooled instance references, Addressables handles or scene ownership handles.

## Release Rules

| Case | Rule |
|---|---|
| Double release | Logical release is idempotent for already released handles. Physical release adapters must not run destructive cleanup twice for the same ownership evidence. |
| Stale handle | Reject stale or already invalid physical evidence. Diagnostics must identify the logical owner/scope/identity and adapter evidence that failed. |
| Missing physical reference | Report explicit adapter failure or no-op success only when an accepted policy says missing physical evidence is already cleaned. Do not recreate or search by Unity name. |
| Release after scope exit | Reject or defer according to the accepted owner/scope policy. Do not clean up content for a mismatched or stale scope. |
| Release failure | Return explicit failed status and diagnostics. Do not silently mark physical cleanup complete. |
| Adapter exception | Convert to explicit adapter failure diagnostics. Do not throw through consumer code as normal control flow in the accepted runtime path. |
| Unsupported release kind | Fail with explicit unsupported-kind diagnostics. Do not infer Destroy, Pooling, Addressables or scene unload behavior from resource strings. |
| Owner mismatch | Reject physical cleanup when adapter ownership evidence does not match `RuntimeReleaseRequest` owner and identity. |

Unity names, hierarchy paths, scene paths and asset paths are diagnostic text only. Functional identity must come from explicit ids, owners, scopes and adapter-owned physical evidence.

## Non-Goals

- no implementation
- no materializer
- no Destroy call
- no pool return implementation
- no Addressables release implementation
- no scene unload implementation
- no actor spawn
- no camera/audio/save/gameplay consumer
- no ContentAnchor physical placement

## Open Questions

- Should physical release run before logical `Released` state is applied, or should the core apply logical release only after adapter success?
- Should missing physical evidence be a failure, a no-op success or policy-specific?
- How should a Pooling adapter represent active lease ownership without leaking pooled instance references into core?
- How should Addressables release evidence be held without exposing Addressables handles to RuntimeContent core?
- Which boundary owns scene-owned content cleanup: RuntimeContent release adapter, Scene Lifecycle, Activity operation or a project policy?
- Should ContentAnchor logical binding cleanup occur before or after physical release in a later F9R-A plan?

## Candidate Cuts After Approval

| Candidate | Type | Purpose | Selection state |
|---|---|---|---|
| F9R-A — ContentAnchor Runtime Binding Re-entry | Audit / plan candidate | Re-enter ContentAnchor binding with materialization and release boundaries explicit. | Not selected automatically. |
| Future materializer/release adapter implementation | Implementation candidate | Implement physical materialization and release adapters only after explicit acceptance. | Not selected automatically. |

F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain blocked until the user explicitly selects a later cut.
