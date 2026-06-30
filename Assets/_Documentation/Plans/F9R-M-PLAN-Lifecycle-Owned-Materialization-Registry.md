# F9R-M — Lifecycle-Owned Materialization Registry Plan

Status: Accepted / Plan / docs-only

Scope: architecture plan only. This cut does not change runtime code, does not add lifecycle auto-materialization and does not authorize F10, F34, gameplay or any consumer module.

## Purpose

F9R-M defines the next ownership boundary required after the F8R/F9R materialization proofs.

The current proof chain can materialize, bind, place, release, preflight, validate, snapshot and roll back explicit authored bridge batches. However, physical materialization evidence is still held by explicit adapter/bridge registries. Route and Activity lifecycle scopes already create/remove logical runtime roots, but removing a logical root must not be treated as proof that all Unity objects, bindings and adapter registries were physically released.

F9R-M plans a lifecycle-owned materialization registry model so future Route/Activity release can own cleanup of materialized content without also introducing automatic materialization.

## Accepted Position

```text
explicit materialization may remain authored/QA/adapter driven
lifecycle-owned registry may own release visibility and cleanup authority
auto-release may be planned before auto-materialization
auto-materialization remains blocked
```

## Baseline Evidence

| Cut | Baseline carried into F9R-M |
|---|---|
| F8R-E | Unity prefab materialization adapter proof exists and can request physical `Object.Destroy` through an explicit release path. |
| F8R-F | Readiness review accepted the explicit proof chain but rejected consumer unlock because lifecycle ownership was still incomplete. |
| F9R-B/F9R-C | ContentAnchor physical placement and composed materialization/binding/placement are proven as explicit adapter paths. |
| F9R-D | Explicit scope release proof exists, but it is not yet a general lifecycle-owned registry. |
| F9R-E/F9R-F | Authored bridge and bridge set expose explicit submit/release only. |
| F9R-G/F9R-H/F9R-I | Preflight, authoring validation and runtime authoring gate prevent invalid batch side effects before materialization. |
| F9R-J | Bridge set diagnostics snapshot is query-only and side-effect free. |
| F9R-L | Bridge set rollback prevents partial active batch state when a later bridge fails after earlier bridges materialized. |

## Problem Statement

The framework currently has three separate meanings that must not be collapsed:

| Layer | Current meaning | Risk if collapsed |
|---|---|---|
| Logical RuntimeContent root | Owner/scope registry for runtime content handles. | Treating root removal as physical object cleanup would hide leaked Unity objects. |
| ContentAnchor binding registry | Logical correlation between runtime content and anchor declaration. | Treating binding cleanup as physical release would let consumers destroy or move objects indirectly. |
| Unity materialization registry | Adapter/bridge-local evidence that a Unity object was created, placed and release was requested. | Keeping it local prevents Route/Activity lifecycle from proving complete cleanup. |

F9R-M resolves only the ownership plan for the third layer. It does not implement the registry.

## Accepted Decisions

| Topic | Decision |
|---|---|
| Registry owner | A future lifecycle-owned materialization registry is owned by a lifecycle scope owner: Session, Route, Activity or Transient. |
| Core purity | RuntimeContent core remains logical. The lifecycle-owned materialization registry is a Unity/adapter-side layer, not a new `UnityEngine` dependency inside RuntimeContent core. |
| Materialization authority | The registry records materializations that were explicitly requested by authored QA, bridge, adapter or later accepted lifecycle code. It does not cause materialization by itself. |
| Release authority | The registry may become the source of truth for lifecycle cleanup. Route/Activity release can ask the registry to release materialized entries before or during logical root cleanup. |
| Identity key | Entries must be keyed by typed runtime identity: owner + runtime content id. GameObject name, hierarchy path, scene path and asset path remain diagnostics only. |
| Physical evidence | Entries may store adapter-side physical evidence required to release or diagnose the object, but consumers must not receive mutation authority through the registry. |
| Release semantics | Release means explicit adapter release was requested and logical/binding registries were updated according to the accepted release policy. Unity `Object.Destroy` remains deferred. |
| Failure semantics | Release failure must be explicit and report owner, content id, adapter/source and count diagnostics. No silent cleanup fallback. |
| Pre-existing content | A lifecycle release must only release entries owned by the lifecycle-owned registry/scope. Pre-existing unrelated content must be preserved. |
| Batch semantics | Batch lifecycle release should reuse the F9R-L principle: partial release/materialization failure must be reported and not hidden. |
| Bridge/bridge set role | Authored bridge/bridge set can remain explicit submit/release surfaces. They must not become implicit Route/Activity lifecycle wiring merely because a registry exists. |

## Proposed Future Shape

Names are provisional until an implementation cut selects them.

| Future artifact | Responsibility | Not allowed to do |
|---|---|---|
| `LifecycleMaterializationRegistry` | Track materialized entries by lifecycle owner and runtime identity. | Instantiate, destroy, bind, place or discover anchors by itself. |
| `LifecycleMaterializedEntry` | Immutable/mostly immutable evidence for one active or released materialization. | Use GameObject name/path as identity. |
| `LifecycleMaterializationReleaseRequest` | Request release for one owner/scope or one identity. | Infer owner from scene hierarchy or object parent. |
| `LifecycleMaterializationReleaseResult` | Report release counts, skipped entries, failures and diagnostics. | Hide adapter failures or convert them to success. |
| `ILifecycleMaterializationRegistryAdapter` | Adapter-facing boundary for Unity registry integration. | Leak consumer-specific APIs into framework core. |

## Lifecycle Release Ordering

Future implementation should use this ordering unless a later ADR explicitly changes it:

```text
Route/Activity exit requested
  -> freeze lifecycle owner identity
  -> query lifecycle-owned materialization registry for owner entries
  -> request physical/adapter release for owned entries
  -> remove ContentAnchor logical bindings for released entries
  -> release RuntimeContent logical handles/root
  -> report aggregate release result
  -> complete lifecycle exit only if blocking failures are absent
```

Important: this orders release. It does not materialize anything automatically.

## Route / Activity Implications

| Scope | Future lifecycle-owned registry implication | Still blocked |
|---|---|---|
| Route | Route exit may eventually release Route-owned materialized entries before Route logical root removal. | Route auto-materialization, camera/audio consumers, gameplay content. |
| Activity | Activity exit may eventually release Activity-owned materialized entries before Activity logical root removal. | Activity auto-materialization, actor spawn, player join, gameplay objects. |
| Session | Session shutdown may eventually release Session-owned entries. | Persistent scene/content policy and global consumer ownership. |
| Transient | QA/explicit tests may keep using transient registries. | General runtime-spawned semantics. |

## Non-goals

- No runtime code change.
- No editor code change.
- No scene, prefab or asset change.
- No automatic Route/Activity materialization.
- No lifecycle runtime auto-wiring.
- No Pause, Camera, Audio, Save, Actor, Pooling, PlayerJoin or gameplay consumer.
- No Addressables integration.
- No pooling integration.
- No new fallback for missing registry, missing release adapter or invalid owner.

## Consumer Unlock Status

| Consumer / track | Status after F9R-M |
|---|---|
| Pause ContentAnchor consumer | Still blocked until lifecycle-owned release is implemented/proven or an explicit narrower owner is accepted. |
| Camera | Still blocked. |
| Audio | Still blocked. |
| Save / Snapshot / Progression | Still blocked from physical object lifetime assumptions. |
| Actor materialization | Still blocked. |
| Pooling / runtime-spawned | Still blocked. |
| Player join | Still blocked. |
| F34 / gameplay | Still blocked. |

## Candidate Next Cuts

F9R-M does not select implementation automatically. If selected explicitly later, the next safe cuts are:

| Candidate | Type | Purpose |
|---|---|---|
| F9R-N — Lifecycle-Owned Registry Contract Proof | Runtime implementation proof | Add a minimal registry contract/result model without Route/Activity auto-wiring. |
| F9R-O — Explicit Bridge Registration Into Lifecycle Registry Proof | Runtime/QA proof | Let explicit bridge materialization register entries into a lifecycle-owned registry while preserving explicit submit/release. |
| F9R-P — Lifecycle Registry Release Plan Proof | Runtime/QA proof | Prove passive owner/scope release plan queries with no release execution. |
| F9R-Q — Lifecycle Registry Release Execution Proof | Runtime/QA proof | Prove explicit execution of a release plan through a caller-provided runtime release executor. |
| F9R-R — Route/Activity Exit Auto-Release Decision | Accepted / Decision / docs-only | Immediate Route/Activity auto-release rejected; composite release gap selected. |
| F9R-S — Explicit Composite Lifecycle Release Executor Proof | Runtime/QA proof candidate | Prove physical adapter/bridge release, ContentAnchor binding cleanup and logical RuntimeContent release through one explicit lifecycle-owned release path. |

## Decision

F9R-M is accepted as the lifecycle-owned materialization registry planning baseline.

The next implementation, if explicitly selected, should start with a minimal registry contract proof and not with Route/Activity auto-materialization.
