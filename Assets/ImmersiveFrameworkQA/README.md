# Immersive Framework QA

This project contains technical QA for the Immersive Framework. The QA source of
truth is the current product contract, not historical cut numbering.

## Principles

```text
QA proves the framework.
QA does not create a parallel runtime.
```

- use public framework APIs and canonical lifecycle composition;
- setup materializes authored configuration;
- fixture owns references/scope;
- regression executes contracts and records evidence;
- orchestrators coordinate already-valid regressions and do not reproduce setup
  or assertions;
- capture baseline before mutation;
- acquire scoped endpoints while the scope is valid;
- cleanup is idempotent and follows ownership in reverse order;
- do not use reflection, service locators, global opportunistic lookup or silent
  fallbacks to make a smoke pass.

## Current domain surfaces

- `Player/`: one Player functional. Setup creates fixtures, Hub opens the Player
  QA scene, and the panel runs the full public Player contract in Play Mode.
  Pause/Input/Gate authoring remains a focused Edit Mode proof in the same
  functional. PauseP1 product-binding assets stay adjacent for the Pause Hub
  entry; Game Flow keeps Activity participation regressions.
- `Camera/`: Camera rig materialization, persistent output authoring,
  canonical C9R authority, ADR-004B negative integrity and ADR-004C owner lifetime
  certification.
- `GameFlow/`: Route/Activity request, reset/restart and lifecycle contracts.
  The focused ADR-005 Pause runtime regression remains adjacent in
  `GameFlow/InternalEditor/QaPauseRuntimeBindingSmoke.cs` because it proves Pause
  runtime authority, PlayerInput/Gate behavior and the Pause + Activity Restart
  interaction on the existing GameFlow lifecycle. It is complementary to the
  static Pause/Input/Gate composition proof in `Player/Scripts/Editor`; there is no
  separate canonical `Pause/` QA domain directory in the current tree.
- `ActivityFlow/`: Activity transaction/readiness behavior.
- `InputMode/`, `Transition/`, `Loading/`, `Reset/`, `Audio/` and other focused
  product domains retain their own canonical technical proofs where those
  directories actually exist.

Historical inventories may list removed or merged smokes. They are evidence of
past state, not the current execution surface.

## Camera QA — current architecture

Camera uses one canonical positive lifecycle plus focused certification runners.

```text
C9M Follow Pipeline
  -> local CameraRigComposer / Follow materialization

C9R Camera Override Authority
  -> positive Player / Activity / Route / Session authority lifecycle

ADR-004C Owner Lifetime Integrity
  -> abnormal component disable/destroy cleanup

ADR-004B Negative Integrity
  -> deterministic negatives, rollback and delegated lifecycle/authoring proof
```

Current executed certification:

```text
C9R      11/11 PASS
ADR004C  10/10 PASS
ADR004B  18/18 PASS
```

### Camera setup

```text
Immersive Framework > QA > Setup > Camera >
Install Camera Override Authority QA
```

The setup repairs the existing C9R fixture and persistent output composition. It
must not create a parallel Camera authority.

### Camera execution order

In one Play Mode session:

```text
1. Camera Override Authority from QA Hub
2. wait for C9R 11/11 and return to Hub
3. Run ADR-004C Owner Lifetime Integrity Certification
4. Run ADR-004B Negative Integrity Certification
```

ADR-004B cases 14-16 consume C9R lifecycle evidence from that Play Mode session.

### Camera certification documents

- `Camera/Documentation/C9R-CAMERA-OVERRIDE-AUTHORITY-QA.md`
- `Camera/Documentation/ADR004C-CAMERA-OWNER-LIFETIME-INTEGRITY.md`
- `Camera/Documentation/ADR004B-CAMERA-NEGATIVE-INTEGRITY-QA.md`
- `Camera/Documentation/CAMERA-QA-CONSOLIDATION-CLOSURE-2026-08-10.md`

### Camera consolidation

The current Camera QA intentionally does not restore historical UX/reflection or
Runtime Host integration smokes whose contract was absorbed by the canonical
surfaces. See the Camera consolidation closure document for the current retained
surface.

A QA-only v10 synthetic Local Player teardown patch addresses redundant
`release-not-found` logging after the functional certification gates. A clean-log
rerun of that hygiene patch is separate from the executed C9R/004B/004C verdicts.

## Player QA canonical architecture

Player is one QA feature with one setup, Hub entry, primary scene and
consolidated panel — the same shape as Audio, Pooling and Lifecycle.

```text
1. Immersive Framework > QA > Setup > Player > Configure Player QA
2. Open Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity
3. Enter Play Mode
4. Open Player QA
5. Run All Player QA
6. Inspect the consolidated PASS / FAIL output
```

`Run Full Player QA` in Edit Mode validates authoring and Pause/Input/Gate
composition. The same menu in Play Mode runs the panel suite, requesting the
Player route from Hub when needed.

See `Player/README.md`.

### Pause/Input/Gate composition certification

The current single-player authoring authority is:

```text
UnityPlayerInputGateAdapter
  -> owns PlayerInput
  -> owns Gameplay Action Map
  -> owns physical Gate writes

PlayerPauseInput
  -> owns Pause Action
  -> derives PlayerInput and Gameplay Action Map from the Gate Adapter
```

Focused Edit Mode regression:

```text
Immersive Framework
  > QA
    > Player
      > Pause
        > Run Pause Input Gate Composition
```

This Edit Mode regression does not replace the Play Mode Pause lifecycle proof.
`GameFlow/InternalEditor/QaPauseRuntimeBindingSmoke.cs` remains responsible for
Pause state transitions, Gate application/restoration, lifecycle release/teardown
and Pause + Activity Restart behavior.

## Identity Authority

Identity QA follows authored-definition authority and stable identity contracts.
Route/Activity authored definitions are not made equivalent merely because text
or scene data matches. Stable IDs are persistence/diagnostic evidence where the
owning product contract defines them.

## Historical QA documents

Documents such as `Documentation/QA-SMOKE-CONSOLIDATION-AUDIT.md` are historical
point-in-time audits. Their old file counts, menus and classifications are not a
request to recreate removed smokes. Current domain documentation and source code
are authoritative for execution.
