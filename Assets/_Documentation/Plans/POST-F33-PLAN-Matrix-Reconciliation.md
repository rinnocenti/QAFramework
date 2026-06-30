# POST-F33 — Matrix Reconciliation Plan

Status: Accepted

## Purpose

Keep the post-F33 roadmap aligned with the capability matrix before any new implementation starts. This plan records what F28-F33 closed, what remains blocked by the matrix and which candidate cuts are valid without selecting a new feature by implication.

## Closed State F28-F33

| Phase | Closed state |
|---|---|
| F28 | Documentation-first roadmap reconciliation and adapter module spine. Official as planning/governance for the Input/Pause path. |
| F29 | Unity Input target ownership proof. Official as declaration/evidence proof; no InputMode runtime behavior. |
| F30 | Passive InputMode identity, request/result and Pause-to-InputMode mapping. Official as framework language; no Unity action-map side effect. |
| F31 | PlayerActor identity and Session `PlayerInputManager` evidence. Official as identity/evidence; no actor spawn, movement or join behavior. |
| F32 | Explicit Unity `PlayerInput` application lane. Official as controlled anticipation; side effects limited to explicit `PlayerInput` adapter/application calls. |
| F33 | Opt-in authored Pause runtime to `PlayerInput` wiring. Official as controlled anticipation; does not select the next implementation phase. |

## POST-F33-B Reclassification

Status: Accepted / closed / docs-only.

POST-F33-B officially reclassifies F28-F33 without reopening them for code and without selecting a new implementation phase.

| Phase | Classification | Summary |
|---|---|---|
| F28 | Official planning/governance | Roadmap reconciliation, dependency ordering and adapter module spine. |
| F29 | Official Unity Input target evidence | Target declarations, diagnostics and authored QA evidence. |
| F30 | Official passive InputMode / Pause request language | Passive mode/request/result contracts and Pause-to-InputMode mapping. |
| F31 | Official PlayerActor identity and Session PlayerInputManager evidence | PlayerActor identity plus `PlayerInput` and Session `PlayerInputManager` evidence. |
| F32 | Controlled anticipation — explicit PlayerInput application lane | Explicit adapter side effects only: `ActivateInput`, `DeactivateInput`, `SwitchCurrentActionMap`. |
| F33 | Controlled anticipation — opt-in Pause runtime to PlayerInput wiring | Authored opt-in bridge from Pause input to Pause/InputMode/`PlayerInput`. |

POST-F33-B keeps F34/gameplay unauthorized. It also keeps camera, audio, save/progression, pooling/runtime-spawned and actor materialization blocked until the F8/F9 materialization and binding blockers are re-audited.

## Not Authorized

- F34 is not selected.
- Gameplay is not selected.
- Camera, audio, save/progression, pooling/runtime-spawned and actor materialization work are not selected.
- No implementation phase may be inferred from F33 closure alone.

## Candidate Cuts

| Candidate | Type | Why it is a candidate | Resolves | Unlocks | ADR before code |
|---|---|---|---|---|---|
| POST-F33-B — Officialize/Reclassify F28-F33 | Docs-only / closed | Completed governance step after the matrix audit. It labels F28-F33 against the original matrix. | Ambiguous official vs anticipated vs experimental reading for the Input/Pause/PlayerInput axis. | Clear entry criteria for the next technical audit. | No code; ADR only if the reclassification changes accepted public architecture. |
| F8R-A — RuntimeContent / ContentAnchor Materialization Audit | Audit-only / baseline accepted | First technical candidate after reclassification because the matrix still blocks consumers on RuntimeContent / ContentAnchor / materialization. | Unknown current sufficiency of runtime root, content handle, materialization request/result and release policy. | F8R-B and F9R-A planning; later camera/audio/save/runtime-spawned/gameplay consumers. | No code; ADR likely after audit if ownership changes. |
| F8R-B — Runtime Root / Handle / Release Policy Plan | Plan / accepted planning baseline | Candidate after F8R-A confirmed the actual gaps. | Missing or inconsistent runtime ownership model for materialized content. | Safe planning for later materialization and release-policy cuts. | ADR accepted; no implementation selected. |
| F8R-C — Runtime Materialization Adapter Boundary Plan | Plan / accepted planning baseline | Candidate after F8R-B/B1 accepted logical root, handle and release policy ownership. | Missing boundary between pure RuntimeContent core and future physical Unity, Addressables or Pooling adapters. | F8R-D release planning, F9R-A binding re-entry and eventual implementation only after explicit approval. | ADR accepted; no implementation selected. |
| F8R-D — Physical Release Adapter Plan | Plan / accepted planning baseline | Candidate after F8R-C/C1 accepted the materialization adapter boundary. | Missing boundary for Destroy, pool return, Addressables release, scene unload and project-owned cleanup policy outside RuntimeContent core. | F9R-A binding re-entry and eventual materializer/release adapter implementation only after explicit approval. | ADR accepted; no implementation selected. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Draft / current planning candidate | Candidate after F8R-A/F8R-B/F8R-C/F8R-D clarify runtime handles, release policy, materialization adapter boundaries and physical release boundaries. | ContentAnchor logical binding uncertainty and placement/lifecycle rules. | Future ContentAnchor physical placement planning only after explicit approval. | ADR proposed; no implementation selected. |
| F8R-E — Unity Prefab Runtime Materialization Adapter Proof | Implemented / selected by user | First physical RuntimeContent adapter proof after accepted F8R-B/F8R-C/F8R-D boundaries. | Absence of a concrete Unity prefab materialization proof and physical Destroy release proof. | Future readiness review for materialization/release implementation. Does not unlock consumers by itself. | No new ADR; implements within accepted F8R-C/F8R-D boundaries. |
| F9R-L — Unity ContentAnchor Materialization Bridge Set Rollback Proof | Closed / PASS | Hardening cut after F8R-F readiness review identified partial batch materialization as a concrete blocker. | Bridge set runtime failure after one or more bridges already materialized. | Safer explicit QA/authored bridge set operation. Does not unlock consumers by itself. | No new ADR; implemented within accepted F8R/F9R boundaries. |
| F9R-M — Lifecycle-Owned Materialization Registry Plan | Accepted / Plan / docs-only | Planning cut after F9R-L. Defines how lifecycle may own materialization registry/release evidence before any Route/Activity auto-materialization. | Lifecycle exit currently cannot prove physical cleanup of local adapter/bridge registries. | Future minimal lifecycle-owned registry contract proof. Does not unlock consumers by itself. | Plan accepted; implementation remains unselected. |
| F9R-N — Lifecycle-Owned Materialization Registry Contract Proof | Ready for smoke / implementation selected | Minimal contract implementation after F9R-M. Adds lifecycle-owned registry/entry/result/status contracts and QA smoke. | Missing typed registry contract for lifecycle-owned materialization evidence. | Future explicit bridge registration into lifecycle registry and later release planning. Does not unlock consumers by itself. | No new ADR; implements within accepted F9R-M plan. |

## Immediate Rule

The current closed governance cut is `POST-F33-B — Officialize/Reclassify F28-F33`, accepted and closed as docs-only.

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` is the accepted audit baseline for the RuntimeContent / ContentAnchor materialization gap.

## F8R-A Current Audit Status

`F8R-A - RuntimeContent / ContentAnchor Materialization Audit` is complete as the current baseline. It did not select implementation and did not authorize F34, gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization.

## F8R-B Current Planning Status

`F8R-B - Runtime Root / Handle / Release Policy Plan` is accepted as the logical ownership baseline. `F8R-B1 - Runtime Root / Handle / Release Policy Acceptance` accepted the ADR. They are docs-only / Plan / ADR and do not implement a materializer, physical release adapter, Content Anchor placement or any consumer.

Implementation remains unselected. F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain blocked until the user explicitly selects a later cut.

## F8R-C Current Planning Status

`F8R-C - Runtime Materialization Adapter Boundary Plan` is accepted as the materialization adapter boundary planning baseline after the accepted F8R-B/F8R-B1 baseline. `F8R-C1 - Runtime Materialization Adapter Boundary Acceptance` accepted the ADR. It defines the boundary between pure RuntimeContent core and future physical Unity, Addressables or Pooling adapters.

Implementation remains unselected. F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain blocked until the user explicitly selects a later cut.

## F8R-D Current Planning Status

`F8R-D - Physical Release Adapter Plan` is accepted as the physical release adapter planning baseline after the accepted F8R-C/F8R-C1 boundary. `F8R-D1 - Physical Release Adapter Acceptance` accepted the ADR. It defines the boundary between logical RuntimeContent release and future physical cleanup adapters for Unity Destroy, Pooling return, Addressables release, scene-owned release and project-owned cleanup policy.

Implementation remains unselected. F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain blocked until the user explicitly selects a later cut.

## F9R-A Current Planning Status

`F9R-A - ContentAnchor Runtime Binding Re-entry` is the current draft planning candidate after the accepted F8R-B/F8R-C/F8R-D boundaries. It defines ContentAnchor runtime binding as logical core behavior only: declaration/discovery evidence, binding request/result, logical ContentAnchor content handles, diagnostics and logical cleanup.

F9R-A does not implement physical placement, materialization, release adapters, pooling, Addressables, actor spawn, camera, audio, save/progression, gameplay or F34. Implementation remains unselected.

## F8R-E Implementation Status

`F8R-E - Unity Prefab Runtime Materialization Adapter Proof` is implemented as the first physical RuntimeContent adapter proof selected explicitly by the user after the accepted F8R-B/F8R-C/F8R-D boundaries.

It adds Unity adapter-side prefab/template instantiation, adapter-side physical evidence, explicit `Object.Destroy` release and a QA canvas smoke.

F8R-E does not implement ContentAnchor physical placement, Addressables, pooling, scene unload, actor spawn, PlayerInputManager join, gameplay, camera, audio or save consumers. Those remain unselected.

## F9R-L Implementation Status

`F9R-L - Unity ContentAnchor Materialization Bridge Set Rollback Proof` is closed / PASS as a hardening cut after the materialization/release readiness review.

It adds explicit rollback to `UnityContentAnchorMaterializationBridgeSet.MaterializeAll`: if a later bridge fails after earlier bridges succeeded, the set releases the already materialized partial batch in reverse order and returns an explicit rollback status.



Smoke closure evidence: the user-provided smoke completed with `passed='True'`, `materializeAll='FailedBridgeMaterializationRolledBack'`, `rollbackReleased='1'`, `failed='1'`, `contentHandles='0'`, `partialMaterializationRolledBack='True'` and `preExistingPreserved='True'`.

F9R-L does not implement lifecycle-owned registries, Route/Activity auto-materialization, F34/gameplay, camera, audio, save/progression, pooling/runtime-spawned, actor materialization or player join. Those remain unselected.


## F9R-M Planning Status

`F9R-M - Lifecycle-Owned Materialization Registry Plan` is accepted as a docs-only planning baseline after F9R-L.

It defines that a future lifecycle-owned materialization registry may own release visibility and cleanup authority for materialized Unity adapter entries, while materialization itself remains explicit and unselected.

Accepted planning rule:

```text
auto-release may be planned before auto-materialization
```

F9R-M does not implement runtime code, editor code, scene changes, Route/Activity auto-materialization, Pause, camera, audio, save/progression, pooling/runtime-spawned, actor materialization, player join, F34 or gameplay.

Candidate next cuts remain implementation candidates only after explicit selection: minimal lifecycle-owned registry contract proof, explicit bridge registration into lifecycle registry proof, registry scope release smoke, or Route/Activity release integration plan.


## F9R-N Implementation Status

`F9R-N - Lifecycle-Owned Materialization Registry Contract Proof` is selected as the minimal implementation after F9R-M.

It adds lifecycle-owned materialization registry contracts and a QA smoke that proves typed registration, duplicate idempotency for the same handle, explicit registry release-state transitions and missing-entry rejection.

F9R-N does not implement bridge/bridge set registration into the lifecycle registry, Route/Activity lifecycle integration, auto-materialization, auto-release, physical release adapter execution, ContentAnchor binding cleanup, Pause, camera, audio, save/progression, pooling/runtime-spawned, actor materialization, player join, F34 or gameplay.

## Superseded Prior Reading

Any earlier post-F33 wording that selected or implied `F34`, gameplay commands, camera, audio, save/progression, pooling/runtime-spawned or actor materialization is not accepted by this plan unless the user later approves it explicitly.
