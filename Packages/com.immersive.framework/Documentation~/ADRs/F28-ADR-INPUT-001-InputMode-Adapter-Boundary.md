# F28-ADR-PLAN-001 — Roadmap Reconciliation and Adapter Module Spine

Status: Accepted / F28A-F28B closed / planning gate  
Phase: F28 — Roadmap Reconciliation and Adapter Module Spine  
Type: Framework Planning / Adapter Module Boundary / Input Positioning

## Context

F27 validated the deferred Pause UIGlobal surface and the narrow PauseToggle input path. The next attempted direction pushed Gate checks into individual input consumers.

That direction was rejected because it treated a missing ownership problem as a local input-consumer problem. The framework still needs a clear completion map for adapter modules, Player/Actor ownership, Unity Input target ownership, InputMode semantics and Pause integration.

The current baseline is frozen at F27D. F27E is cancelled.

## Decision

F28 is not an InputMode implementation phase.

F28 is the roadmap correction phase that defines the adapter/module spine from the stable framework core to future product-facing modules.

The canonical ordering is:

```text
Framework lifecycle core
  -> Unity build surface evidence
  -> adapter/module ownership map
  -> Player/Actor/Input target ownership
  -> typed InputMode semantics
  -> Pause requests InputMode
  -> later Camera/Audio/Save/RuntimeSpawned/Gameplay adapters
```

Runtime implementation resumes only after this planning phase identifies the next concrete owner, dependency chain, package/asset placement and smoke target.

## Positive Scope

F28 owns these planning outputs:

```text
completion dependency map
adapter module taxonomy
module placement rules
PlayerInput ownership decision path
InputMode position in the module graph
Pause/InputMode integration plan
next implementation phase entry criteria
```

## Canonical Tracks

| Track | Owns |
|---|---|
| Framework Core / Contracts | Existing lifecycle contracts, typed identities, state, diagnostics, policies and request/result language. |
| Unity Build Surface | Minimal Unity scenes, surfaces and QA wiring that prove framework contracts. |
| Adapter Modules | Optional modules that connect product/gameplay systems to framework contracts without redefining them. |
| Project Assets | Game-specific prefabs, scenes, UI art, player prefabs and concrete product configuration. |
| External Packages | Unity official modules, optional packages, third-party tools and project-specific assets consumed by adapters. |

## Adapter Module Families

F28 recognizes these future families as separate lanes:

```text
Player / Actor
Unity Input
InputMode
Pause Integration
Camera
Audio
Save / Progression
Runtime Spawned / Pooling
Gameplay Capabilities
```

Each family must declare owner, dependency order and placement before implementation.

## InputMode Position

InputMode remains a required future boundary, but it is downstream of Player/Input ownership decisions.

Candidate modes:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

`InputMode` is typed framework language. Unity action-map names are adapter configuration.

## Pause Direction

The accepted direction remains:

```text
Running:
  InputMode = Gameplay

Paused:
  InputMode = PauseOverlay
  UI input remains available
  PauseToggle / Cancel remains available
  gameplay command maps do not drive gameplay
```

Pause may request a mode change after InputMode exists. Pause does not own `PlayerInput`, action-map strings or player lifecycle.

## Gate Position

Gate remains passive admission and hard-lock language.

Gate should be used for:

```text
admission diagnostics
request guards
exceptional blocks
stale/foreign/in-flight safety checks
```

Gate is not the normal implementation path for pausing ordinary gameplay input.

## Consequences

- F27E remains cancelled.
- The next cut is documentation/planning, not contracts/runtime.
- Adapter module work is treated as a progressive roadmap lane, not as a single input patch.
- Player/Input ownership must be decided before Pause drives action-map behavior.
- Camera, Audio, Save, RuntimeSpawned and Gameplay adapters must not be pulled into core as side effects of solving Pause/Input.

## F28 Cut Order

| Cut | Name | Output |
|---|---|---|
| F28A | Frozen Baseline Reconciliation | Authoritative read of package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Closed: ordered dependency graph for remaining product-completion tracks. |
| F28C | Adapter Module Taxonomy | Next: module families, owner kind, placement rule and dependency category. |
| F28D | Player / Actor / Input Ownership Plan | Player object, `PlayerInput`, player/actor adapter placement and first target proof. |
| F28E | InputMode and Pause Integration Plan | Typed InputMode semantics and Pause-driven mode requests after ownership is clear. |
| F28F | Next Implementation Closeout | Next code phase, entry criteria, smoke target and file placement rules. |


## F28A Closure

F28A closes the frozen baseline reconciliation.

Accepted baseline:

```text
F24-F27 are frozen evidence.
F27E is cancelled.
F28 is roadmap reconciliation before runtime implementation resumes.
Assets/ and Packages/com.immersive.framework/ are both source-of-truth boundaries for their respective scopes.
```

F28B is the next cut and must produce a dependency map before any adapter/module implementation starts.

## F28B Dependency Decision

F28B closes the completion dependency map.

The accepted order is:

```text
F28C adapter module taxonomy
  -> F28D Player / Actor / Unity Input ownership
  -> F28E InputMode and Pause integration
  -> F28F next implementation selection
```

InputMode is explicitly downstream of Unity Input target ownership. Pause integration is explicitly downstream of InputMode.

Camera, Audio, Save / Progression, RuntimeSpawned / Pooling and Gameplay Capabilities are separate adapter lanes. They may be planned in parallel after taxonomy, but implementation must respect their family-specific blockers.

F28B reference:

```text
Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md
```

