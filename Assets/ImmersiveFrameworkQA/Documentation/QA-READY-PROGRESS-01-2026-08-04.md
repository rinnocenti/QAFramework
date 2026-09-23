# QA-READY-PROGRESS-01 — Participant-Aware Readiness Loading Progress

Date: 2026-08-04  
Type: technical QA regression  
Mode: Play Mode / host-owned integration  
Framework prerequisite: `afb1e781d2eaaf76143479f7d5d4e9d3c3edff21`

## Objective

Prove the positive `WaitCovered` participant-aware Loading progression through the
real `FrameworkRuntimeHost` request path:

```text
technical completion below 100%
→ 0/4 Required
→ Optional failure remains non-blocking and does not advance progress
→ 1/4
→ 2/4
→ 3/4
→ 4/4 + aggregate Ready = 100%
→ Loading Hide
→ Transition reveal
→ gate release
```

## Fixture

The regression reuses `QaActivityEntryReadinessFixture` for the runtime Activity,
content scene, request ownership and authority restoration. A supplemental fixture
adds three Required participants and one Optional participant to the existing
Required participant, producing the exact set:

```text
Required: 4
Optional: 1
```

The Activity uses:

```text
EntryReadinessPolicy: WaitCovered
VisualTransitionMode: FadeWithLoading
TransitionGateMode: InputInteractionAndGameplay
Activity content: QA_IF_READY_04_DirectPoliciesContent
```

The Transition and Loading adapters are resolved only inside the official host's
persistent runtime scene. No global object lookup is used.

## Evidence contract

`QaLoadingPresentationEvidenceEntry` gains immutable typed progress fields and the
adapter publishes each evidence entry through `PresentationEvidenceRecorded`.
The existing `Show + Update* + Hide` grammar remains unchanged; a new
`RequireDeterminateUpdates` projection validates supported, finite, monotonic
Update requests.

The regression uses one local causal sequence to join:

- Loading progress request evidence;
- Loading hidden application evidence;
- Transition visible/hidden state-change evidence.

No `Task.Delay`, timeout, frame polling or log parsing is used. The only
`NextFrameAsync` calls are destruction-propagation checks owned by fixture cleanup.

## Expected result

```text
[QA_READY_PROGRESS_01]
status='Passed'
cases='32'
required='4'
optional='1'
optionalOutcome='FailedNonBlocking'
ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease'
```

## Out of scope

- Required failure, invalidation, cancellation and stale occurrence paths;
- Route Startup Activity;
- Game Application Startup Activity;
- FIRSTGAME integration;
- Loading visual redesign;
- changes to the framework package.
