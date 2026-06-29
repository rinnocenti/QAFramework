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
| F8R-B — Runtime Root / Handle / Release Policy Plan | Plan / current planning candidate | Candidate after F8R-A confirmed the actual gaps. | Missing or inconsistent runtime ownership model for materialized content. | Safe planning for later materialization and release-policy cuts. | Yes before code if it defines new public runtime ownership. |
| F9R-A — ContentAnchor Runtime Binding Re-entry | Audit / plan | Candidate after F8R-A/F8R-B clarify runtime handles and release policy. | ContentAnchor binding uncertainty and placement/lifecycle rules. | Pause content materialization and later consumer attachment. | Yes before code if it changes binding semantics. |

## Immediate Rule

The current closed governance cut is `POST-F33-B — Officialize/Reclassify F28-F33`, accepted and closed as docs-only.

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` is the accepted audit baseline for the RuntimeContent / ContentAnchor materialization gap.

## F8R-A Current Audit Status

`F8R-A - RuntimeContent / ContentAnchor Materialization Audit` is complete as the current baseline. It did not select implementation and did not authorize F34, gameplay, camera, audio, save/progression, pooling/runtime-spawned or actor materialization.

## F8R-B Current Planning Status

`F8R-B - Runtime Root / Handle / Release Policy Plan` is the current planning candidate. It is docs-only / Plan / ADR and does not implement a materializer, physical release adapter, Content Anchor placement or any consumer.

## Superseded Prior Reading

Any earlier post-F33 wording that selected or implied `F34`, gameplay commands, camera, audio, save/progression, pooling/runtime-spawned or actor materialization is not accepted by this plan unless the user later approves it explicitly.
