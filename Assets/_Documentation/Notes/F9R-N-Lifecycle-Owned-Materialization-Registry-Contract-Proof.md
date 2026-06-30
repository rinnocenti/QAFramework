# F9R-N — Lifecycle-Owned Materialization Registry Contract Proof

Status: Ready for smoke

Scope: RuntimeContent contract proof plus QA smoke. This cut does not connect Route/Activity lifecycle exit, does not materialize content automatically and does not unlock F10, F34, gameplay or subsystem consumers.

## Purpose

F9R-N implements the minimal lifecycle-owned materialization registry contract selected after F9R-M.

The goal is to represent this fact without relying on GameObject name, hierarchy path, scene path or adapter-local registry state:

```text
this materialized RuntimeContent identity belongs to this lifecycle owner/scope
```

This is evidence/ownership language only. It does not instantiate prefabs, destroy objects, bind Content Anchors, remove ContentAnchor bindings, release RuntimeContent handles or wire Route/Activity automatically.

## Added Runtime Contracts

| Artifact | Responsibility | Not allowed to do |
|---|---|---|
| `LifecycleMaterializationRegistry` | Explicit registry of lifecycle-owned materialized entries by typed runtime identity. | Instantiate, destroy, pool, unload scenes, bind anchors or call lifecycle exit. |
| `LifecycleMaterializedEntry` | Evidence that one materialized `RuntimeContentHandle` is owned by one lifecycle owner. | Use GameObject name/path as identity or expose physical mutation authority. |
| `LifecycleMaterializationEntryState` | Entry state vocabulary: `Active`, `ReleaseRequested`, `Released`, `ReleaseFailed`. | Represent Unity `Object.Destroy` completion. |
| `LifecycleMaterializationRegistryOperationStatus` | Operation result status for register/release-state transitions. | Hide missing entries, duplicate entries or invalid transitions. |
| `LifecycleMaterializationRegistryOperationResult` | Diagnostic result for one registry operation. | Execute physical or logical release. |

## QA Smoke

New QA button:

```text
Run Lifecycle Materialization Registry Contract Smoke
```

Expected success log:

```text
QA Lifecycle Materialization Registry Contract Smoke step completed.
step='lifecycle-materialization-registry-contract'
passed='True'
register='SucceededRegistered'
duplicate='SucceededAlreadyRegistered'
duplicateConflict='RejectedDuplicateEntry'
releaseRequest='SucceededReleaseRequested'
releaseComplete='SucceededReleased'
missingRelease='RejectedMissingEntry'
entries='1'
active='0'
releaseRequested='0'
released='1'
releaseFailed='0'
typedIdentity='True'
registryOwnsEvidenceOnly='True'
physicalRelease='False'
logicalRuntimeContentRelease='False'
contentAnchorBindingCleanup='False'
automaticLifecycleWiring='False'
routeActivityAutoMaterialization='False'
routeActivityAutoRelease='False'
```

## Accepted Boundaries

- The registry records typed runtime identity and lifecycle ownership.
- Duplicate registration of the same handle is stable/idempotent.
- Duplicate registration of a different handle with the same identity is rejected.
- Release request and release completion are explicit state transitions in the registry only.
- Missing release requests are explicit failures.
- The registry does not remove logical RuntimeContent handles.
- The registry does not request Unity physical release.
- The registry does not remove ContentAnchor bindings.
- The registry does not create lifecycle-owned Route/Activity wiring.

## Non-goals

- No Route/Activity auto-materialization.
- No Route/Activity auto-release.
- No lifecycle exit integration.
- No physical release adapter execution.
- No ContentAnchor binding cleanup.
- No bridge/bridge set registration into the lifecycle registry yet.
- No Addressables.
- No pooling.
- No actor spawn.
- No player join.
- No camera, audio, save/progression or gameplay consumer.

## Next Valid Cuts

| Candidate | Purpose |
|---|---|
| F9R-O — Explicit Bridge Registration Into Lifecycle Registry Proof | Let authored bridge/bridge set explicitly register successful materializations into the lifecycle registry without Route/Activity auto-wiring. |
| F9R-P — Lifecycle Registry Scope Release Plan/Smoke | Build/query a release plan or release-state transition batch from the lifecycle registry. |
| F9R-Q — Route/Activity Release Integration Plan | Decide whether lifecycle exit may call lifecycle-owned registry release before logical root removal. |

## Decision

F9R-N is ready for smoke as the minimal lifecycle-owned registry contract proof.

It advances the F9R ownership model but does not unlock consumers or automatic lifecycle materialization.
