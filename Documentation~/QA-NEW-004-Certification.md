# QA-NEW-004 — Camera Selection Continuity and Replacement

## Contract

`Route A [CameraPresentationSelections = Camera A]` transitions to
`Route B [CameraPresentationSelections = empty]` while preserving the exact
effective occurrence and normal `CameraRequest` after Route A exits and its
Primary Scene unloads.

This public QA intentionally does not inspect `RuntimeContent` internals. The
package test owns the proof of Session owner/scope, handle and
`RuntimeContentIdentity`.

The certification preserves the Route A -> C and Activity D-A -> D-B legs,
then proves CAMERA-037-E through the public Activity lifecycle:
Activity E-A [Camera A] -> Activity E-C [Camera C, same Output].

## Composition

1. Run `QA > QA-NEW-004 > Create Isolated Composition` once.
2. Run `QA > QA-NEW-004 > Activate Game Application`.
3. Run `QA > QA-NEW-004 > Add Route C Replacement Composition`.
4. Run `QA > QA-NEW-004 > Add Activity D Continuity Composition`.
5. Run `QA > QA-NEW-004 > Add Activity E Replacement Composition`.
6. Open `Assets/QA-NEW-004/Scenes/QA_NEW_004_Persistent.unity`.
7. Enter Play Mode.

The setup command creates only assets under `Assets/QA-NEW-004`, adds its three
scenes to the Build Settings and never overwrites an existing QA-NEW-004
composition.

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
- Activity E-A becomes active with Camera A as the Session-owned normal winner;
- the Activity E-C request is submitted, completed and succeeds;
- Activity E-A reports its exact public exit to E-C;
- Camera C remains live after E-A teardown with a fresh occurrence token and
  request ID;
- Camera A's request is no longer admitted after commit;
- Camera C is the applied normal winner and `observedWinnerDrop='False'`.

## Required package evidence

Run `CameraPresentationRouteSelectionContinuityTests` in package Editor tests.
Its continuity case additionally proves the same materialization handle,
`RuntimeContentIdentity`, full `CameraRequest`, Session owner/scope, removal of
the Route A scope and absence of the occurrence under Route B.

Run `CameraPresentationActivitySelectionContinuityTests` as well. Its three
transition contracts prove A -> B empty continuity, A -> C successful
replacement and pre-commit rollback with exact handle,
`RuntimeContentIdentity`, request and winner preservation.
