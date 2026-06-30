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
| F9R-B — Unity ContentAnchor Physical Placement Adapter Proof | Implemented / selected by user | First explicit Unity ContentAnchor physical placement proof after logical binding/materialization boundaries. | Missing adapter-side physical placement evidence. | Bridge and bridge-set proofs only after explicit selection. Does not unlock consumers by itself. | No new ADR; implements within accepted F9R-A/F8R-C/F8R-D boundaries. |
| F9R-C — Unity ContentAnchor Materialization Pipeline Proof | Implemented / selected by user | Composes materialization, logical binding and physical placement. | Missing explicit composition proof. | Explicit release and bridge proofs only after explicit selection. Does not unlock consumers by itself. | No new ADR. |
| F9R-D — Unity ContentAnchor Materialization Scope Release Proof | Implemented / selected by user | Adds explicit cleanup by runtime owner for materialized ContentAnchor content. | Missing scope release proof. | Authored bridge proofs only after explicit selection. Does not unlock consumers by itself. | No new ADR. |
| F9R-E — Unity ContentAnchor Materialization Bridge Proof | Implemented / selected by user | Adds authored opt-in submit/release bridge over the validated path. | Missing scene-facing explicit bridge. | Bridge set proof only after explicit selection. Does not unlock Route/Activity auto-materialization. | No new ADR. |
| F9R-F — Unity ContentAnchor Materialization Bridge Set Proof | Implemented / selected by user | Adds authored opt-in multi-bridge batch submit/release. | Missing explicit batch bridge. | Preflight/validation hardening only after explicit selection. Does not unlock lifecycle wiring. | No new ADR. |
| F9R-G — Unity ContentAnchor Materialization Bridge Set Preflight Proof | Implemented / selected by user | Adds preflight-before-side-effects for bridge-set batches. | Partial batch materialization risk. | Authoring validation gate only after explicit selection. Does not unlock lifecycle wiring. | No new ADR. |
| F9R-H — Unity ContentAnchor Materialization Authoring Validation Proof | Implemented / selected by user | Adds authoring validation for bridge and bridge set surfaces. | Invalid authored configuration before runtime submission. | Runtime authoring gate only after explicit selection. Does not unlock lifecycle wiring. | No new ADR. |
| F9R-I — Unity ContentAnchor Materialization Runtime Authoring Gate Proof | Implemented / selected by user | Reuses authoring validation as bridge-set runtime gate before preflight and side effects. | Runtime submission bypassing authoring validation. | Diagnostics snapshot proof only after explicit selection. Does not unlock lifecycle wiring. | No new ADR. |
| F9R-J — Unity ContentAnchor Materialization Diagnostics Snapshot Proof | Closed / PASS / selected by user | Adds query-only diagnostics snapshot for authored bridge-set state. | Missing common read model for QA/Inspector/runtime diagnostics. | F9R closeout only. Does not select F10, F34 or consumers. | No new ADR. |
| F9R-K — F9R Closeout / Documentation Sync | Accepted / docs-only | Synchronizes project/package docs with F9R-H/F9R-I/F9R-J and records F9R-J PASS. | Documentation drift after F9R-J. | No implementation axis selected. | No code; no ADR. |

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

## F9R-B through F9R-J Implementation Status

F9R-B through F9R-J are implemented/closed as explicitly selected RuntimeContent + ContentAnchor materialization and diagnostics proofs.

Closed sequence:

| Phase | Status | Result |
|---|---|---|
| F9R-B | Implemented | Physical placement adapter proof. |
| F9R-C | Implemented | Materialization + logical binding + physical placement pipeline proof. |
| F9R-D | Implemented | Explicit scope release proof. |
| F9R-E | Implemented | Authored opt-in bridge proof. |
| F9R-F | Implemented | Authored opt-in bridge set proof. |
| F9R-G | Implemented | Bridge set preflight proof. |
| F9R-H | Implemented | Authoring validation proof. |
| F9R-I | Implemented | Runtime authoring gate proof. |
| F9R-J | Closed / PASS | Query-only diagnostics snapshot smoke PASS. |

F9R-B through F9R-J do not implement automatic lifecycle wiring, Route/Activity auto-materialization, Addressables, pooling, actor spawn, PlayerInputManager join, gameplay, camera, audio, save/progression consumers or F34.

## F9R-K Closeout Status

`F9R-K - F9R Closeout / Documentation Sync` is accepted as docs-only. It synchronizes indexes and records F9R-J smoke PASS. It selects no new implementation axis.

## Superseded Prior Reading

Any earlier post-F33 wording that selected or implied `F34`, gameplay commands, camera, audio, save/progression, pooling/runtime-spawned or actor materialization is not accepted by this plan unless the user later approves it explicitly.
