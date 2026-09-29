# QA-NEW-004 — Camera Selection Continuity and Replacement

> **Retired by CAMERA-038-I.** This document records historical evidence for
> the removed Presentation/Request and Route/Activity Camera selection path. Its
> scenario and setup source were removed because they certified that obsolete
> contract. Existing generated scenes, prefabs and assets remain migration input
> for CAMERA-038-J and are not executable validation for IF-ADR-038.

## Contract

`Route A [CameraPresentationSelections = Camera A]` transitions to
`Route B [CameraPresentationSelections = empty]` while preserving the exact
effective occurrence and normal `CameraRequest` after Route A exits and its
Primary Scene unloads.

This public QA intentionally does not inspect `RuntimeContent` internals. The
package test owns the proof of Session owner/scope, handle and
`RuntimeContentIdentity`.

The certification executes the public B/C/D/E lifecycle matrix:

- Route A -> Route B empty preserves Camera A;
- Route A -> Route C replaces Camera A with Camera C on the same Output;
- Activity D-A -> Activity D-B empty preserves Camera A;
- Activity E-A -> Activity E-C replaces Camera A with Camera C on the same
  Output.

CAMERA-037-F keeps this public integration proof and adds package-only coverage
for the nested pending Route + Startup Activity composition rule.

## Composition

1. Run `QA > QA-NEW-004 > Create Isolated Composition` once.
2. Run `QA > QA-NEW-004 > Activate Game Application`.
3. Run `QA > QA-NEW-004 > Add Route C Replacement Composition`.
4. Run `QA > QA-NEW-004 > Add Activity D Continuity Composition`.
5. Run `QA > QA-NEW-004 > Add Activity E Replacement Composition`.
6. Open `Assets/QA-NEW-004/Scenes/QA_NEW_004_Persistent.unity`.
7. Enter Play Mode.

The setup commands create or repair only assets under `Assets/QA-NEW-004` and
register the generated scenes in Build Settings idempotently. The C/D/E menus
preserve the base composition; generated extension scenes are rebuilt when
needed so a partial setup run is repaired without retaining stale references.

## PASS evidence

The terminal log must contain:

```text
[QA-NEW-004] status='Passed' verdict='PASS'
```

The scenario requires, before PASS:

- public Route B request succeeds;
- Route A receives its exact public exit callback;
- Route A Primary Scene unloads;
- Route B becomes the active Route scene and declares zero selections;
- the same `QaNew004CameraOccurrenceProbe` reference and occurrence token remain;
- the same complete normal `CameraRequest` remains the winner in the same
  `CameraOutputContext`;
- the winner is reapplied after transition force-default ends;
- cleanup returns to Route A without recreating the selected occurrence.
- Route C is submitted/completed/succeeded, Route A really exits, Camera C has
  a fresh occurrence/request, Camera A is no longer admitted and the normal
  winner never drops;
- Activity D-A becomes active through its public lifecycle, Activity D-B is
  submitted/completed/succeeded, Activity D-A really exits and the exact same
  Camera A occurrence/request remains;
- Activity E-A becomes active with Camera A as the Session-owned normal winner;
- the Activity E-C request is submitted, completed and succeeds;
- Activity E-A reports its exact public exit to E-C;
- Camera C remains live after E-A teardown with a fresh occurrence token and
  request ID;
- Camera A's request is no longer admitted after commit;
- Camera C is the applied normal winner and `observedWinnerDrop='False'`.

## Required package evidence

Run `CameraPresentationRouteSelectionContinuityTests` in package Editor tests.
Its continuity/replacement/rollback cases additionally prove exact handle,
`RuntimeContentIdentity`, complete `CameraRequest`, Session owner/scope, dead
Route-scope removal and absence under the incoming Route. CAMERA-037-F also
proves that same-Output Route + Startup Activity pending selections are rejected
without residual Activity candidate/request while different-Output pending
selections coexist and roll back independently.

Run `CameraPresentationActivitySelectionContinuityTests` as well. Its three
transition contracts, plus the initial Activity-entry ownership case, prove
A -> B empty continuity, A -> C successful replacement and pre-commit rollback
with exact handle, `RuntimeContentIdentity`, request and winner preservation.
