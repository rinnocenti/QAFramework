# QA-NEW-004 — Route Camera Selection Continuity

## Contract

`Route A [CameraPresentationSelections = Camera A]` transitions to
`Route B [CameraPresentationSelections = empty]` while preserving the exact
effective occurrence and normal `CameraRequest` after Route A exits and its
Primary Scene unloads.

This public QA intentionally does not inspect `RuntimeContent` internals. The
package test owns the proof of Session owner/scope, handle and
`RuntimeContentIdentity`.

## Composition

1. Run `QA > QA-NEW-004 > Create Isolated Composition` once.
2. Run `QA > QA-NEW-004 > Activate Game Application`.
3. Open `Assets/QA-NEW-004/Scenes/QA_NEW_004_Persistent.unity`.
4. Enter Play Mode.

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

## Required package evidence

Run `CameraPresentationRouteSelectionContinuityTests` in package Editor tests.
Its continuity case additionally proves the same materialization handle,
`RuntimeContentIdentity`, full `CameraRequest`, Session owner/scope, removal of
the Route A scope and absence of the occurrence under Route B.
