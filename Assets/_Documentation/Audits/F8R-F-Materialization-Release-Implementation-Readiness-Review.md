# F8R-F — Materialization / Release Implementation Readiness Review

Status: Reviewed / audit-only / consumers remain blocked

Scope: documentation and static package review. No runtime, editor, prefab, scene, asmdef or package metadata changes are authorized by this file.

## 1. Purpose

F8R-F reviews whether the implemented RuntimeContent + ContentAnchor materialization/release chain from F8R-E and F9R-B through F9R-J is ready to unlock later consumers.

Reviewed chain:

```text
RuntimeContent logical root/context/handle
  -> Unity prefab materialization adapter proof
  -> logical ContentAnchor binding
  -> Unity physical placement adapter proof
  -> explicit scope release pipeline
  -> authored bridge / bridge set
  -> bridge set preflight
  -> authoring validation
  -> runtime authoring gate
  -> query-only diagnostics snapshot
```

## 2. Verdict

The chain is ready for **explicit QA/authored proof usage**.

The chain is **not ready to unlock automatic lifecycle consumers**.

Blocked consumers remain blocked:

- F34/gameplay;
- camera;
- audio;
- save/progression;
- actor materialization;
- pooling/runtime-spawned integration;
- player join / `PlayerInputManager.JoinPlayer`;
- Route/Activity automatic materialization.

## 3. Evidence reviewed

| Area | Evidence | Review result |
|---|---|---|
| Runtime owner | `FrameworkRuntimeHost` initializes `RuntimeContentRuntime` and `RuntimeContentAnchorBinding`. | Present and host-owned. |
| Route/Activity scopes | `RouteLifecycleRuntime` and `ActivityFlowRuntime` create/remove logical runtime scope roots. | Present for logical ownership. |
| Logical handle lifecycle | `RuntimeContentRuntime` creates materialization requests, applies materialization results, creates release requests and performs logical release. | Present. |
| Unity materialization adapter | `UnityPrefabRuntimeMaterializationAdapter` instantiates an explicit prefab/template and records adapter-side evidence. | Present, explicit, experimental. |
| Unity physical release adapter | `UnityObjectRuntimeReleaseAdapter` requests `Object.Destroy` for adapter-created evidence. | Present, explicit, experimental. |
| Physical evidence registry | `UnityRuntimeMaterializedObjectRegistry` is local and explicit, not global. | Correct for proof, insufficient for automatic lifecycle. |
| Logical ContentAnchor binding | `RuntimeContentAnchorBinding` binds registered RuntimeContent handles to a supplied `ContentAnchorSet`. | Present, logical only. |
| Physical placement | `UnityContentAnchorPlacementAdapter` reparents materialized evidence under an explicit anchor Transform. | Present, adapter-side. |
| Composed pipeline | `UnityContentAnchorMaterializationPipeline` materializes, applies logical registration, binds and places; failure paths attempt rollback. | Good proof-level composition. |
| Scope release pipeline | `UnityContentAnchorMaterializationScopeReleasePipeline` performs physical release, logical release and ContentAnchor binding cleanup for one explicit context. | Present, explicit. |
| Authored bridge | `UnityContentAnchorMaterializationBridge` exposes explicit authored submit/release. | Present, opt-in. |
| Bridge set | `UnityContentAnchorMaterializationBridgeSet` batches explicit bridge submit/release. | Present, opt-in. |
| Preflight | Bridge set preflight blocks duplicate materialization keys and invalid bridge materialization before batch side effects. | Present. |
| Authoring validation | `UnityContentAnchorMaterializationAuthoringValidator` validates bridge/bridge set authoring and editor surfaces use it. | Present. |
| Runtime authoring gate | Bridge set runs authoring validation before materialization preflight. | Present. |
| Diagnostics snapshot | Bridge set diagnostics snapshot reads authoring/runtime status without submitting materialization/release side effects. | Present and query-only. |

## 4. Positive findings

### 4.1 Boundary remains clean enough for F9 proof work

The implemented physical operations are still adapter-side:

- prefab instantiation stays in `UnityPrefabRuntimeMaterializationAdapter`;
- `Object.Destroy` stays in `UnityObjectRuntimeReleaseAdapter`;
- Transform parenting stays in `UnityContentAnchorPlacementAdapter`;
- logical binding remains in `RuntimeContentAnchorBinding`;
- logical runtime handle state remains in `RuntimeContentRuntime`.

This respects the F8R-B/F8R-C/F8R-D boundary: pure RuntimeContent language does not store `GameObject`, `Transform`, pool handles or Addressables handles.

### 4.2 Explicit ownership is preserved

The current materialization path requires explicit calls through QA/authored bridge surfaces. It does not silently bind itself to Route enter, Activity enter, Pause, Camera, Audio, Save, Actor or gameplay consumers.

### 4.3 Release proof exists

The project now has an explicit release composition:

```text
UnityObjectRuntimeReleaseAdapter.Release
  -> RuntimeContentRuntime.ApplyReleaseResult
  -> FrameworkRuntimeHost.UnbindContentAnchorRuntimeOwner
```

This is enough to prove the shape of release, but not enough to make release automatic.

### 4.4 Query-only diagnostics are correctly separated

F9R-J correctly adds diagnostics read models without calling materialize, release, bind or placement. This is safe to keep as QA/Inspector evidence.

## 5. Blocking findings

### 5.1 Route/Activity scope removal does not execute physical release

`RouteLifecycleRuntime` and `ActivityFlowRuntime` remove logical scope roots and clean logical ContentAnchor bindings when leaving a Route or Activity. That is not the same as releasing physical Unity objects created by materialization adapters.

Physical release currently requires an explicit bridge/pipeline release call with access to the local `UnityRuntimeMaterializedObjectRegistry`.

**Impact:** automatic Route/Activity materialization remains unsafe. A consumer could create physical objects that are not physically released by ordinary lifecycle root removal.

### 5.2 Physical object registry is local, not lifecycle-owned

`UnityRuntimeMaterializedObjectRegistry` is intentionally local to explicit adapters/bridges. That is correct for proofs, but the lifecycle runtime cannot discover every physical registry created by arbitrary authored bridge sets.

**Impact:** no framework-level automatic cleanup can be claimed yet.

### 5.3 Bridge set materialization is not fully transactional after preflight

Bridge set preflight blocks known invalid states before side effects, but if a later bridge fails during actual materialization after earlier bridges succeeded, the bridge set returns failure without a visible set-level rollback of the earlier successful bridge materializations.

**Impact:** the bridge set is acceptable as an explicit QA/authored proof, but it is not yet consumer-grade batch materialization.

### 5.4 Physical release uses `Object.Destroy`, which is deferred by Unity

The release adapter records physical release requests and calls `Object.Destroy`. Unity destruction is not immediate in ordinary play mode semantics.

**Impact:** diagnostics should continue to report `physicalReleaseRequested`, `registryActive` and logical handle/binding state rather than claiming immediate object absence unless a later smoke explicitly validates that after Unity destruction has completed.

### 5.5 API status remains experimental

The reviewed RuntimeContent and ContentAnchor materialization classes still declare experimental/internal API status. That is correct for current phase proof work, but it means consumers should not be built against this as stable public API yet.

## 6. Readiness matrix

| Capability | Current readiness | Consumer unlock? | Notes |
|---|---|---|---|
| Runtime root / handle / release policy | Ready as logical core language | Partial | Can support future plans. |
| Unity prefab materialization proof | Ready as explicit proof | No | Not lifecycle-owned globally. |
| Physical release proof | Ready as explicit proof | No | Needs lifecycle-owned registry/release policy before automatic use. |
| ContentAnchor logical binding | Ready as logical proof | Partial | Safe for explicit bridge/pipeline paths. |
| ContentAnchor physical placement | Ready as explicit proof | No | Still adapter-side proof, not consumer service. |
| Bridge set preflight | Ready as proof hardening | No | Needs transactional rollback before consumer-grade batch use. |
| Runtime authoring gate | Ready | Partial | Good guardrail; not sufficient for lifecycle safety. |
| Diagnostics snapshot | Ready | Yes, for QA/Inspector only | Query-only. |
| Route/Activity automatic materialization | Not ready | No | Still blocked. |
| Pause materialization consumer | Not ready | No | Needs lifecycle ownership and safe release. |
| Camera/audio/save/actor/gameplay consumers | Not ready | No | Remain blocked by release/lifecycle ownership. |

## 7. Decision

F8R-F closes with this decision:

```text
F8R-E through F9R-J prove explicit materialization, placement, release and diagnostics.
They do not yet unlock consumer implementation.
The next technical work must harden lifecycle/release ownership or batch transactional behavior before consumers are selected.
```

## 8. Candidate follow-ups, not selected by this review

This review does not select the next implementation cut. If the user selects a follow-up, the valid candidates are:

| Candidate | Type | Why it exists |
|---|---|---|
| F9R-L — Bridge Set Transactional Rollback Proof | Implementation hardening | Close the partial side-effect risk when one bridge in a batch fails after earlier materialization succeeded. |
| F9R-M — Lifecycle-Owned Materialization Registry Plan | Docs/ADR first | Define how Route/Activity lifecycle can own physical registries before any auto-materialization. |
| F10 Snapshot/Save foundation | Consumer foundation | Only safe if it stays data-only and does not consume runtime materialization. |
| F10 Pause ContentAnchor consumer | Blocked | Must wait for lifecycle-owned release or remain explicit authored proof only. |

## 9. Non-goals

F8R-F does not:

- implement code;
- change scenes, prefabs or QA objects;
- add a runtime lifecycle hook;
- add Route/Activity auto-materialization;
- add Addressables;
- add pooling;
- add actor spawn;
- add player join;
- add camera/audio/save/gameplay consumers.

## 10. Closeout status

```text
IF-FW-F8R-F — REVIEWED / AUDIT-ONLY
Outcome: explicit proof chain accepted, consumer unlock rejected.
```
