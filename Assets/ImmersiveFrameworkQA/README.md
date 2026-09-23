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
- `Camera/`: Camera rig materialization, ADR-026 Shared/Split topology,
  output-scoped authority, ADR-004B negative integrity and ADR-004C owner lifetime
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

The current Camera flow is:

```text
Subject(s)
  -> CameraPresentationRuntime
  -> CameraRigComposer
  -> CameraRequest
  -> explicit CameraOutputSession
  -> Unity Camera + CinemachineBrain
```

Session declares physical Outputs. Route and Activity declare Presentations.
Player Slot -> Output and Player Slot -> Presentation are explicit Session
bindings. `PlayerInputManager` owns `Camera.rect`. Persistent Content is not
Camera topology authority.

Historical ADR-004/026/028 rails, including override installation, shared
composition and 028-C/028-D menus, are not current certification. Their dated
records stay historical.

### Camera execution order

```text
Immersive Framework > QA > Camera > Run Full Camera QA
```

The aggregate runs, in order:

```text
structural Presentation contract
Subject + Transaction          17/17
two-Output Player lifecycle
CAMERA-032-C Game Flow         16/16
canonical baseline restore
```

The structural contract checks that removed product types stayed removed,
`CameraPresentationDefinition` serializes reusable intent only, and
`CameraPresentationRuntime` is neither a MonoBehaviour nor a ScriptableObject.
The aggregate emits `CAMERA_032_FULL_CERTIFIED` only after those gates and the
canonical restore. Unity execution of this aggregate is still pending.

Setup for the aggregate is the canonical baseline guard. It removes historical
scene-owned Camera roots from Persistent Content and restores QA Hub. There is
no override-authority installer.

### CAMERA-032 Player Output lifecycle certification

The current IF-ADR-032 two-Output Player topology has a dedicated product-level
Play Mode certification. It does not use the historical scene-owned
`PlayerCameraOutputPolicyAuthoring` path.

```text
Immersive Framework > QA > Camera >
Run CAMERA-032 Player Output Lifecycle Certification
```

The certification authors a temporary GameApplication Camera Session with two
physical Output prefabs and two ExplicitSelection ThirdPerson Presentations. It
proves the lifecycle `0 -> 1 -> 2 -> 1 -> 2 -> 0`: zero-Player Session
Default continuity, single-player full-screen ThirdPerson with the unassociated
Player Output dormant, independent two-player split Outputs and Cinemachine
channels, Leave recomposition back to full-screen without Unity Input System
"Player has no camera associated" errors, ThirdPerson continuity for the
surviving Player, fresh rejoin, and restoration of Default continuity after the
final Leave.

The setup restores the canonical Shared Camera QA baseline after the focused
certification. Package NUnit tests are supporting implementation checks only;
QAFramework Play Mode evidence is the certification gate.

### CAMERA-032 Game Flow lifecycle certification

The current IF-ADR-032 Route/Activity migration has a dedicated two-boot
certification for transition force-default continuity and lifecycle restoration:

```text
Immersive Framework > QA > Camera >
Run CAMERA-032 Game Flow Lifecycle Certification
```

The first boot authors one Session Presentation below Route and Activity
Presentations and proves:

```text
Activity -> Route restoration
Route replacement -> Session restoration
force-default applies the Output Default without mutating normal requests
force-default release restores the current normal winner
outgoing Route primary scene may unload before the replacement winner resumes
```

Activity restoration and force-default are intentionally separate proofs. Activity
clear proves only Activity -> Route restoration. Force-default continuity is
proven on Route replacement, where an actual primary-scene load/unload keeps the
covered physical lifecycle observable.

The certification generates two temporary camera-free Route scenes so no
scene-owned Unity Camera or AudioListener can mask the Session Output being
certified. Those scenes are added to Build Settings only for the run and removed
during canonical cleanup/recovery.

The second boot repeats the lifecycle without a Session Presentation and proves
Route replacement -> physical Output Default. Each boot owns 8 ordered cases;
the focused gate is therefore 16/16 when both fallback modes pass and the
canonical Shared Camera QA baseline is restored.

Latest Unity execution on 2026-09-23:

```text
Session fallback     8/8 PASS
Default fallback     8/8 PASS
Focused total       16/16 PASS
Canonical restore          PASS
Verdict                    CAMERA_032_GAMEFLOW_LIFECYCLE_CERTIFIED
```

The generated Route scenes intentionally contain no scene-owned Camera authority.
Unity may emit temporary `There are no audio listeners in the scene` warnings
before Persistent Content and the Session Output materialize; those warnings are
non-blocking for this Camera certification and were present in the successful
16/16 run.

This certification uses the current GameApplication Camera Session,
`CameraPresentationDefinition`, Route/Activity Presentation arrays and persistent
public Game Flow request triggers. It does not certify the historical
`CameraSharedComposition` lifecycle path.

### CAMERA-032 Subject + Transaction certification

The remaining IF-ADR-032 Subject occurrence, stale-evidence and transaction
contracts have a focused Edit Mode certification:

```text
Immersive Framework > QA > Camera >
Run CAMERA-032 Subject + Transaction Certification
```

This is intentionally an isolated Camera-domain gate: it uses transient preview
scenes and does not modify the canonical QA Hub, Persistent Content,
GameApplication or Build Settings. It drives `CameraPresentationRuntime`
directly. It does not use a scene composition adapter.

The authored gate owns 17 deterministic cases:

```text
Subject / stale evidence                         9
Transaction integrity                            8
--------------------------------------------------
Focused total                                   17
```

Subject coverage includes `0 -> 1 -> many -> 1 -> 0`, required-target release,
Fixed with zero Subjects, fresh same-logical-id Subject occurrence, stale
availability, stale explicit selection, stale membership tokens, stale
Presentation rollback and stale membership rollback.

Transaction coverage includes Rig apply failure, unsupported target projection,
request-admission rollback, force-default preservation across failed admission,
request-release rollback, successful retry after rollback, explicit critical
rollback failure, and owner-safe release between two live Presentation
occurrences.

Fault injection is test-local and uses the existing runtime boundaries. The
certification does not add a Framework service, manager, public fault-injection
API or alternate arbitration path.

Latest Unity execution on 2026-09-23:

```text
[CAMERA-032-SUBJECT-TRANSACTION-CERTIFICATION]
status='Passed'
verdict='CAMERA_032_SUBJECT_TRANSACTION_CERTIFIED'
subjectCases='9/9'
transactionCases='8/8'
totalCases='17/17'
cleanup='TransientPreviewScenesClosed'
```

The successful run closes the focused IF-ADR-032 Subject / Presentation and
Transaction Integrity technical evidence represented by these 17 cases. Final
IF-ADR-032 closure still depends on CAMERA-032-F legacy cleanup, remaining
authoring/static reconciliation and aggregate recertification.

### Camera certification documents

- `Camera/Documentation/C9R-CAMERA-OVERRIDE-AUTHORITY-QA.md`
- `Camera/Documentation/ADR004C-CAMERA-OWNER-LIFETIME-INTEGRITY.md`
- `Camera/Documentation/ADR004B-CAMERA-NEGATIVE-INTEGRITY-QA.md`
- `Camera/Documentation/CAMERA-QA-CONSOLIDATION-CLOSURE-2026-08-10.md`

### Camera consolidation

The current Camera QA intentionally does not restore historical reflection-based
or Local Player Camera-request smokes. See the Camera consolidation closure
document for the retained surface and ADR-026 migration classification.

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
