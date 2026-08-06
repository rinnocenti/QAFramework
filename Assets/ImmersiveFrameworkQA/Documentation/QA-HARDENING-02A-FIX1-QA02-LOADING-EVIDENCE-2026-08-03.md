# QA-HARDENING-02A-FIX1 — QA-02 Loading Evidence Metadata

## Objective

Correct the QA-02 direct synthetic Loading evidence validation introduced by QA-HARDENING-02A.

## Root cause

The previous validation compared `QaLoadingPresentationEvidenceEntry.Detail` with the fourth factory argument used to create the request. The adapter records the typed `LoadingSurfaceRequest.Detail` property. Those values are not interchangeable, so a valid `RequestReceived` entry failed at index 0.

## Correction

QA-02 now validates each evidence entry against the actual typed `LoadingSurfaceRequest` used for that action:

- `Action` against `request.Action`;
- `RequestedVisible` against `request.ShouldBeVisible`;
- `Source` against `request.Source`;
- `Detail` against `request.Detail`;
- visual state and result status against the expected Show/Hide phase;
- strictly increasing evidence sequence;
- exact counters for the direct synthetic Show/Hide protocol.

QA-02 does not use the host-oriented `QaLoadingPresentationEvidenceGrammar` for this direct adapter exercise. QA-03 continues to use that shared variable grammar for host-produced `Show + Update* + Hide` evidence.

## Preserved contracts

- QA-01 remains unchanged and its validated PASS remains valid.
- QA-02 remains `RuntimeSynthetic` with 26 cases in the same order.
- no frame was added;
- the only `NextFrameAsync` remaining in QA-02 confirms Unity destruction propagation;
- no package, scene, prefab, asset, setting, or asmdef is changed.

## Validation

Run only QA-02 after applying this overlay. Expected result:

```text
[IF_READY_04_QA_PRESENTATION_EVIDENCE]
status='Passed'
cases='26'
fixtureMode='RuntimeSynthetic'
loadingEvidence='6'
loadingLifecycleEvidence='6'
loadingUpdateRequests='0'
loadingUpdateEvidence='0'
```
