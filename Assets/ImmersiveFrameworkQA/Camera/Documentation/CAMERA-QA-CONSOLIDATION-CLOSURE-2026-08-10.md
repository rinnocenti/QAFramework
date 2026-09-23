# Camera QA Consolidation Closure — updated 2026-09-21

Status: **current operational map after ADR-026 migration**

## Retained surfaces

```text
C9M Follow Pipeline
  local CameraRigComposer materialization

Camera Output Session Binding Authoring Regression
  output identity, references and Default rig validation

Persistent Camera Presentation Composition Regression
  Output identity plus explicit Composition -> Rig -> Request -> Output ownership

C9R / ADR-026 canonical fixture
  Shared 10-case subject/framing/occurrence proof,
  Split 8-case Multi-Output proof and generic authority

ADR-004C Owner Lifetime Integrity
  abnormal component publication lifetime

ADR-004B Negative Integrity
  deterministic negatives, rollback and delegated lifecycle evidence
```

## Audit classification

```text
keep
  Hub -> Route -> Activity -> canonical Camera scene -> fixture -> Hub
  Activity/Route/Session precedence and ADR-004B/004C cleanup evidence

migrate
  single-output composition -> 1..N Outputs with exact unique identities
  reflection validation -> public topology/authoring contracts
  Player baseline restoration -> authored Output Default restoration

remove
  QaC9RLocalPlayerCameraRequestBinding
  LocalPlayerCameraRequestPublisher dependencies
  LocalPlayer owner/lifetime assertions
  assumption that a second valid Output is an error

extend
  Shared two-player join/leave/rejoin occurrence proof
  Group membership plus Split physical layout and output-isolation proof
```

The owning surface remains `QAFramework/Camera`; the Framework package owns the
runtime contracts and is consumed read-only by this QA cut.

## Current execution

```text
Immersive Framework > QA > Camera > Run Full Camera QA
  -> persistent Camera structural Edit Mode regression (10/10)
  -> author Shared topology in Edit Mode
  -> fresh Shared Play Mode / Hub / canonical fixture / ADR-004B / ADR-004C
  -> fresh SceneProvided Play Mode proof
  -> return to Edit Mode and author Split topology
  -> fresh Split Play Mode / Hub / canonical fixture
  -> return to Edit Mode and restore persisted canonical Shared topology
  -> verify restore and emit one final multidimensional diagnostic
```

CAMERA-030 is explicit about fixture ownership: generic Player default and
alternate Presentations publish no framing radius and therefore exercise the
Group fallback; the Camera-owned replacement Presentation publishes `1.25` and
exercises explicit Subject framing. Default rigs remain fallback-only. Session
and Output B requests use explicit non-default Follow rigs.

The Full established count is `10 structural + 11 generic + 10 Shared + 8
Split = 39`. SceneProvided is an additional mandatory phase reported
separately. CAMERA-028-C and CAMERA-028-D remain focused certifications and are
not represented as implicit Full execution.

Historical inventories remain point-in-time evidence. Historical C9R/ADR-004
PASS lines must not be used as ADR-026 certification. A fresh Unity run is still
required.
