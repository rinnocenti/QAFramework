# F28B — Completion Dependency Map

Status: Closed / docs-only  
Phase: F28 — Roadmap Reconciliation and Adapter Module Spine  
Scope: dependency ordering, blocker map and first implementation candidates  
Runtime changes: none

## Purpose

F28B turns the F28A freeze reading into an explicit dependency map for the remaining completion work.

The cut does not choose a runtime implementation yet. It defines which decisions must exist before Player, InputMode, Pause integration, Camera, Audio, Save, RuntimeSpawned and Gameplay modules can move forward.

Canonical flow:

```text
frozen framework evidence
  -> dependency map
  -> adapter module taxonomy
  -> player / actor / input ownership
  -> InputMode semantics
  -> Pause requests InputMode
  -> optional subsystem adapters
  -> gameplay modules
```

## Baseline Dependencies Already Available

The following are treated as available foundations from the frozen baseline:

| Foundation | Current role | Constraint |
|---|---|---|
| Route / Activity lifecycle | Owns framework lifecycle and request flow. | Adapter modules consume it; they do not replace it. |
| Gate | Admission, hard-lock and diagnostic language. | Not the normal way to pause ordinary input consumers. |
| Transition / Loading | Operation presentation and progress language. | Visual policy remains Unity build surface or adapter, not core gameplay. |
| Pause state | Logical paused/unpaused state and snapshot. | Pause may request effects, but does not own input targets or player lifetime. |
| Global UI surface | Unity-facing proof for persistent Pause/loading UI surfaces. | Evidence surface, not the full product UI system. |
| Snapshot / Preferences / Progression Save | Split save boundaries and backend ports. | Backend choice is replaceable; JSON is an adapter proof, not the contract. |
| Content Anchor / RuntimeContent | Placement, binding, handle and release language. | Consumers must not create ad hoc roots to bypass ownership. |
| Activity scene operation/loading progress | Proven Activity content operation and progress surface. | Does not imply gameplay module ownership. |

## Ordered Completion Graph

### Level 0 — Frozen Evidence

```text
F24 Unity Build Surface
F25 Activity Scene Operation
F26 Activity Discovery / Loading Progress
F27 Pause UIGlobal + narrow PauseToggle
F27E cancelled
```

This level is evidence. It proves Unity-facing wiring and current QA surfaces, but it is not a gameplay module layer.

### Level 1 — Adapter Module Classification

Before runtime resumes, the project needs a small taxonomy that answers:

```text
what is framework core;
what is Unity build surface;
what is optional adapter module;
what is product asset/config;
what is external package dependency.
```

This is the direct input for F28C.

### Level 2 — Player / Actor Ownership

Player and Actor work must define:

```text
who owns the player GameObject lifetime;
which route/activity scope can request or retain a player;
how the player becomes a participant without becoming Route/Activity lifecycle owner;
how Actor materialization relates to RuntimeContent handles;
how reset/release/snapshot participation is attached later.
```

This level blocks Unity Input target ownership and most gameplay modules.

### Level 3 — Unity Input Target Ownership

Unity Input work must define:

```text
who supplies PlayerInput or equivalent targets;
which targets belong to player gameplay;
which targets belong to persistent/global UI;
how input actions are adapted without making action-map strings canonical framework identity;
how duplicate PauseToggle callback behavior remains narrow and deterministic.
```

This level blocks applied InputMode behavior.

### Level 4 — InputMode Semantics

InputMode can define typed framework state only after target ownership exists.

Candidate framework modes remain:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

These are typed framework meanings. Unity action maps are adapter configuration.

### Level 5 — Pause / InputMode Integration

Pause integration can happen after InputMode exists:

```text
Running -> request Gameplay
Paused  -> request PauseOverlay
```

Pause does not own `PlayerInput`, Unity action maps, player lifetime or gameplay command dispatch. It requests a mode transition through the selected owner.

### Level 6 — Optional Subsystem Adapters

Camera, Audio, Save backends, RuntimeSpawned/Pooling and Gameplay modules start after the module taxonomy and their specific blockers are clear.

These modules may progress in separate lanes once their prerequisites are satisfied. They must not become hidden framework core.

## Blocker Map by Family

| Family | Blocked by | Blocks / enables | First acceptable proof |
|---|---|---|---|
| Adapter Module Taxonomy | F28B dependency map. | All later adapter placement decisions. | Documentation table of owner kinds, dependency categories and placement rules. |
| Player / Actor | RuntimeContent ownership, object entry/reset foundations, adapter taxonomy. | Unity Input target ownership, gameplay modules, camera-follow targets. | A player/actor ownership plan, not a prefab implementation. |
| Unity Input | Player target ownership, global UI input target ownership. | Applied InputMode and Pause action-map behavior. | A target ownership plan that separates player gameplay input from persistent UI input. |
| InputMode | Unity Input target path and typed mode semantics. | Pause-driven gameplay command blocking without Gate scattering. | Typed mode contract/plan with adapter responsibility isolated. |
| Pause Integration | InputMode owner and target split. | Product pause behavior and gameplay command suspension. | Pause requests mode changes; it does not own action maps. |
| Camera | Content Anchor binding, RuntimeContent handle/release, optional player/actor targets. | Route/Activity/player camera adapters. | Camera as adapter consumer; no global static authority expansion. |
| Audio | Route/Activity/Pause lifecycle evidence and project policy boundary. | BGM/SFX/listener adapters. | Lifecycle consumer plan; project audio policy outside core. |
| Save / Progression | Snapshot/progression ports and backend placement decision. | JSON proof backend and future premium backend swap. | Backend adapter plan with unchanged progression port. |
| RuntimeSpawned / Pooling | Runtime roots, handles, release policy, pooling package boundary. | Actor/projectile/prefab materialization modules. | Decide simple prefab materializer before pooled materializer unless F28F chooses otherwise. |
| Gameplay Capabilities | Player/Actor and RuntimeSpawned foundations. | Inventory/combat/projectile/damage/attributes. | Gameplay stays optional module or project layer; no core lifecycle ownership. |

## Parallelization Rules

Some work can be planned in parallel after F28C, but implementation must respect blockers.

| Lane | Can plan after | Can implement after |
|---|---|---|
| Save backend adapter | F28C taxonomy | Save/progression backend placement rule is accepted. |
| Audio lifecycle adapter | F28C taxonomy | Lifecycle consumer boundary and project policy split are accepted. |
| Camera adapter | F28C taxonomy | Content Anchor/RuntimeContent placement and target dependency are accepted. |
| RuntimeSpawned materializer | F28C taxonomy | Runtime root/handle/release ownership proof is selected. |
| Player/Input/InputMode | F28C taxonomy | F28D and F28E ownership decisions are accepted. |
| Gameplay capabilities | F28C taxonomy | Player/Actor and RuntimeSpawned foundations exist. |

## Stop Conditions

Stop the cut if it tries to:

```text
create PlayerInput ownership before Player/Actor ownership is documented;
create InputMode runtime before Unity Input targets are owned;
make Pause own action maps;
make Gate the normal input pause mechanism;
create camera/audio/save/player registries to bypass module ownership;
create service locator behavior under adapter terminology;
use Unity names, scene paths or GameObject names as canonical identity;
put project prefabs or product configuration into framework core;
make optional external packages mandatory without an explicit adapter decision.
```

## First Implementation Candidates

F28B does not choose the next runtime phase. It narrows the candidates for F28F:

| Candidate | Why it is plausible | Why it is not chosen yet |
|---|---|---|
| Adapter Module Foundation | It creates the vocabulary needed by all optional module lanes. | F28C must define taxonomy first. |
| Player / Actor Ownership | It unlocks Unity Input and gameplay. | It needs placement rules and runtime owner language confirmed. |
| Save Backend Adapter | It is relatively independent from Player/Input. | It still needs adapter placement and backend swap rules. |
| RuntimeSpawned Simple Prefab Materializer | It unlocks Actor/projectile/pooling later. | It must not bypass root/handle/release policy. |
| InputMode | It solves the Pause/Input problem directly. | It is blocked by Player/Input target ownership. |

## F28B Closure Decision

F28B closes with these decisions:

```text
F28C is still the next cut.
F28C must define adapter module taxonomy before code resumes.
InputMode is confirmed as downstream of Player/Input target ownership.
Pause integration is confirmed as downstream of InputMode.
Camera, Audio, Save, RuntimeSpawned and Gameplay are separate adapter lanes.
No runtime code should be added before F28F selects a concrete implementation phase.
```

## Files Affected

```text
Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md
Assets/_Documentation/Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md
Assets/_Documentation/README.md
Packages/com.immersive.framework/Documentation~/README.md
Packages/com.immersive.framework/Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
Packages/com.immersive.framework/Documentation~/ADRs/ADR-INDEX.md
Packages/com.immersive.framework/Documentation~/ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md
```
