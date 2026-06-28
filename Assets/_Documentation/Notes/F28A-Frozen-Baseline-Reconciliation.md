# F28A — Frozen Baseline Reconciliation

## Status

Closed / documentation-only / no runtime changes

## Purpose

F28A establishes the current project/package state as the authoritative baseline for the next planning cuts.

This cut does not implement a module. It turns the post-F27 freeze into a readable baseline so F28B can build the dependency map without rediscovering the same boundary questions.

## Source of Truth

The current source of truth is the project archive that contains both boundaries:

```text
Assets/
Packages/com.immersive.framework/
```

Reading rule:

```text
Assets/ = project-facing operational source for scenes, QA assets, project documentation and product configuration
Packages/com.immersive.framework/ = framework source for contracts, lifecycle, runtime, diagnostics and generic authoring
```

No previous F28 patch is considered canonical unless it is present in this source tree.

## Frozen Baseline

F24-F27 are treated as the current frozen Unity-facing baseline.

| Phase | Baseline role | F28A reading |
|---|---|---|
| F24 | Unity build surface and lifecycle wiring | Closed surface evidence. It proves Transition, Loading and UIGlobal wiring through Unity assets. It is not a gameplay module phase. |
| F25 | Activity content scene composition and operation planning | Closed Activity operation baseline. Activity visual policy, content load/release and ledger ownership are framework lifecycle concerns. |
| F26 | Activity discovery and loading progress | Closed loading progress path. Progress and loading presentation are surface/adapter evidence, not a replacement for lifecycle ownership. |
| F27 | Pause UIGlobal surface and narrow PauseToggle input | Frozen after F27D. Pause surface and narrow PauseToggle evidence are valid. F27E is cancelled. |

## F27E Cancellation Rule

F27E must remain cancelled.

The rejected direction was:

```text
ordinary input consumers query Gate individually
```

The accepted direction is:

```text
Pause state
  -> typed InputMode boundary
  -> Unity Input adapter applies concrete input behavior
```

Gate remains passive admission, hard-lock and diagnostics language. Gate is not the normal pause/input control path for every consumer.

## Current Boundary Reading

| Boundary | Current reading |
|---|---|
| Framework Core | Owns lifecycle language, Route/Activity ownership, Transition, Loading, Pause, Gate, Save/Snapshot/Preferences, RuntimeContent, ContentAnchor and diagnostics contracts. |
| Unity Build Surface | Owns minimal authored Unity evidence under `Assets/ImmersiveFrameworkQA` and project docs. It proves framework contracts but does not become product gameplay. |
| Adapter Modules | Future lane. They connect gameplay/subsystems to framework contracts after ownership and placement are explicit. |
| Project Assets | Concrete prefabs, scenes, materials, player objects, UI art and product-specific settings stay under `Assets/_Project` or QA/sandbox equivalents. |
| External Packages | Unity official packages, optional third-party packages and personal/local tools remain separate from canonical framework setup. |

## Diagnostic Cleanup Reading

The project has already reduced repeated text-normalization diagnostics through the shared runtime extension:

```text
Packages/com.immersive.framework/Runtime/Common/FrameworkStringExtensions.cs
```

This is the canonical local helper for framework text normalization. New runtime diagnostics should reuse existing common helpers instead of adding duplicate private normalization methods when the common helper already covers the case.

## What F28A Confirms

F28A confirms:

```text
1. F24-F27 are the current frozen baseline.
2. F27E is cancelled and must not be revived as a local consumer patch.
3. F28 is a positive roadmap phase, not an InputMode implementation phase.
4. Adapter modules are future lanes, not one monolithic runtime registry.
5. InputMode remains downstream of Player/Input ownership.
6. Camera, Audio, Save, RuntimeSpawned and Gameplay adapters must not be pulled into framework core while solving Pause/Input.
7. Runtime work resumes only after F28F chooses one next implementation phase with entry criteria and smoke target.
```

## What F28A Opens

F28A opens F28B.

F28B must produce an ordered dependency map for:

```text
Runtime root / ownership
Player / Actor ownership
Unity Input target ownership
InputMode
Pause integration
Camera
Audio
Save / Progression
Runtime Spawned / Pooling
Gameplay capabilities
```

F28B should answer what must exist before each lane can implement code.

## Non-Goals

F28A does not create:

```text
runtime contracts
adapter module descriptors
PlayerInput ownership code
InputMode runtime service
action-map switching
Pause-driven command blocking
Camera/audio/actor/save adapters
QA buttons
asmdefs
Unity scenes or assets
```

## Next Cut

Next cut:

```text
F28B — Completion Dependency Map
```

Expected output:

```text
one dependency graph
ordered blocker list
first implementation candidates
clear stop conditions for premature consumers
```
