# C9R — Camera Authority and ADR-026 QA

Status: **canonical Camera lifecycle and topology regression**
Last updated: **2026-09-21**

## Product contract

Ordinary players publish subject availability. They do not own or publish a
`CameraRequest`. A `CameraSharedComposition` selects the active subjects from the
joined-player set, presents them through an explicit Group rig, publishes its own
output-scoped request, and rejects stale player occurrences after leave/rejoin.

```text
Subject(s) -> Composition -> Rig -> CameraRequest -> Output
```

Generic Camera authority is output-scoped:

```text
Output Default   baseline
Activity         100
Route            200
Session          300
```

Higher precedence wins; release or owner exit restores the next valid request and
ultimately the authored Output Default rig.

The Output Default rig is a physical fallback only. Session and independent
Output B requests use explicit non-default Follow rigs; the Shared Composition
uses its explicit Group rig.

## Existing execution rail

```text
QA Hub
  -> RouteTrigger_Camera__Override_Authority
  -> QA_PlayerCameraArbitrationRoute
  -> startup Activity
  -> QA_PlayerCameraArbitration scene
  -> QaCameraOverrideAuthorityFixture
  -> back-to-Hub Route
```

The fixture is repaired in place. There is no second Camera service, context,
manager or parallel orchestration path.

## ADR-026 Shared phase

The persistent topology authors two distinct Outputs with Fixed Default rigs.
Shared Camera Composition binds directly to Output A and owns a distinct Group
Composition Rig. Output B remains independently addressable. The fixture proves:

- P1 join creates one live subject and target-group member;
- P1-A, P2 and fresh P1-B leave framing radius unspecified and therefore prove
  the Group behavior fallback radius;
- the Camera-owned `QA_SharedCameraReplacementPresentation` publishes radius
  `1.25`, which is carried through Camera Subject evidence into the exact
  TargetGroup member;
- P2 join creates the second member;
- P1 leave removes its exact occurrence while P2 remains;
- P1 rejoin creates a new occurrence, uses fallback framing and never revives stale P1-A;
- Composition membership, Group rig, Cinemachine camera and Output identity stay stable;
- Composition request appears with subjects and is released at zero subjects;
- cleanup leaves both players and closes joining.

## ADR-026 Split/Multi-Output phase

The second fresh boot retains the two explicit physical Outputs. It proves
distinct Camera/Brain/default-rig materialization, direct Output identity,
missing-output rejection, independent request arbitration and restoration to
each Output's own Default rig. CAMERA-028-D separately proves that
PlayerInputManager owns PlayerInput.camera, split recomposition and Camera.rect.

## Setup and execution

```text
Immersive Framework > QA > Setup > Camera > Prepare ADR-026 Shared Phase
Immersive Framework > QA > Setup > Camera > Prepare ADR-026 Split Phase
Immersive Framework > QA > Camera > Run Full Camera QA
```

The full runner first executes the 10-case persistent Camera structural
regression in Edit Mode. It then executes a fresh Shared boot, a fresh
SceneProvided boot and a fresh Split boot. After Split it exits Play Mode,
restores and verifies the persisted canonical Shared topology, and only then
emits terminal certification. Generic Activity/Route/Session authority,
ADR-004B negative integrity and ADR-004C owner lifetime remain part of the
Shared boot. CAMERA-028-C and CAMERA-028-D remain focused certifications and
are not implicitly counted by Full.

Established cardinality is derived from executed evidence:

```text
Structural 10 + Generic 11 + Shared 10 + Split 8 = 39
```

Expected terminal evidence is causal and machine-readable:

```text
[QA_CAMERA_ADR026] status='Passed' phase='Shared' cases='10/10' subjectFraming='ExplicitAndFallbackPASS' ...
[QA_CAMERA_ADR026] status='Passed' phase='Split' cases='8/8' ...
[QA_CAMERA_FULL] status='Completed' verdict='CAMERA QA CERTIFIED' structuralCases='10/10' sharedCases='10/10' genericCases='11/11' splitCases='8/8' sceneProvided='PASS' canonicalRestore='PASS' cleanup='CanonicalSharedRestored'
```

Do not record PASS from static inspection. Certification requires a clean Unity
compile/import and the full runtime run.
