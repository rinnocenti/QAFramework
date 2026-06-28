# F28 Plan — Roadmap Reconciliation and Adapter Module Spine

## Status

Planned / documentation-first / no runtime changes

## Purpose

F28 converts the post-F27 freeze into a clean implementation roadmap.

The phase creates the planning spine between the stable framework lifecycle core and the future product-facing modules. It defines what the next implementation tracks are, who owns each track, what each track depends on, and where each track should live.

F28 is a positive planning phase:

```text
frozen framework core
  -> completion dependency map
  -> adapter/module ownership map
  -> ordered implementation tracks
  -> next runtime cut with clear entry criteria
```

The immediate goal is not to add another local fix. The goal is to stop reactive implementation and produce a progressive plan that can be followed phase by phase.

## Starting Point

F24-F27 are treated as the current frozen baseline.

| Phase | Frozen result |
|---|---|
| F24 | Unity build surface and lifecycle wiring validated. |
| F25 | Activity content scene composition and operation planning stabilized. |
| F26 | Activity discovery and loading progress closed. |
| F27 | Pause UIGlobal surface and narrow PauseToggle input path validated; F27E cancelled. |

F27E was cancelled because ordinary input consumers should not each become responsible for querying Gate just to make Pause work. The next work must decide the larger module/input ownership chain before any further input behavior is implemented.

## What F28 Is

F28 is the roadmap correction phase for completion work.

It owns:

```text
completion dependency ordering
adapter/module taxonomy
project/package/module placement rules
PlayerInput ownership decision path
InputMode positioning inside the module graph
Pause/InputMode integration plan
next implementation phase selection
```

F28 produces documentation and planning artifacts first. Runtime code resumes only after the plan identifies the next concrete owner, dependency chain and smoke target.

## Canonical Tracks From F28 Forward

| Track | Owns | First F28 output |
|---|---|---|
| Framework Core / Contracts | Existing lifecycle language, typed identities, state, diagnostics, policies and request/result contracts. | Confirm what is already stable and what must not be redefined by modules. |
| Unity Build Surface | Minimal Unity-facing surfaces that prove framework contracts through scenes, QA objects and authored adapters. | Preserve the F24-F27 surfaces as evidence, not as gameplay modules. |
| Adapter Modules | Optional modules that connect product/gameplay systems to framework contracts. | Define module families, ownership and dependency order. |
| Project Assets | Game-specific prefabs, scenes, UI art, player prefabs and product configuration. | Keep concrete product assets outside canonical package contracts. |
| External Packages | Unity official modules and optional third-party systems consumed by adapters. | Mark dependencies as official Unity, optional package, external tool or project-specific asset. |

## Adapter Module Families

F28 treats adapters as a set of future lanes, not a single InputMode patch.

| Family | Purpose | Depends on | First decision needed |
|---|---|---|---|
| Player / Actor | Provides concrete participants, player object ownership and runtime actor targets. | Activity lifecycle, runtime materialization, object entry/reset. | Who owns the player object and player lifetime. |
| Unity Input | Adapts typed input language to Unity Input System targets. | Player/Input target ownership. | Who supplies `PlayerInput` or equivalent targets. |
| InputMode | Owns typed mode state such as Gameplay and PauseOverlay. | Unity Input adapter placement and UI/gameplay target split. | Whether mode state is core-only first or immediately applied by adapter. |
| Pause Integration | Requests mode changes from Pause state. | InputMode boundary. | How PauseOverlay keeps UI/PauseToggle available while gameplay commands stop. |
| Camera | Adapts camera selection/binding to framework lifecycle. | Content Anchor binding, player/actor targets where needed. | Whether camera is package adapter, project adapter or separate module. |
| Audio | Adapts BGM/SFX/listener behavior to Route/Activity/Pause lifecycle. | Session/Route/Activity ownership and optional audio package placement. | Which audio behavior is framework adapter vs project policy. |
| Save / Progression | Adapts snapshot/preferences/progression contracts to replaceable backends. | Snapshot and progression save ports. | Which backend is initial proof and how premium backend swaps later. |
| Runtime Spawned / Pooling | Adapts prefab/pool materialization to runtime handles/release policy. | Runtime root, content handle and pooling package boundary. | Which materializer is first: simple prefab or pooled. |
| Gameplay Capabilities | Inventory, combat, projectile, damage and attributes. | Player/actor/runtime-spawned foundations. | Which gameplay module is product-specific and which contract is reusable. |

## Progressive Cut Matrix

| Cut | Name | Status | Output |
|---|---|---|---|
| F28A | Frozen Baseline Reconciliation | Planned | One authoritative reading of the current frozen baseline, including package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Planned | Ordered dependency graph for the remaining product-completion tracks. Identifies what must precede Player, InputMode, Pause integration, Camera, Audio, Save, RuntimeSpawned and Gameplay. |
| F28C | Adapter Module Taxonomy | Planned | Defines module families, owner kind, placement rule and dependency category. This is still documentation/plan, not runtime registration. |
| F28D | Player / Actor / Input Ownership Plan | Planned | Decides the ownership path for `PlayerInput`, player object lifetime, player/actor adapter placement and the first input target proof. |
| F28E | InputMode and Pause Integration Plan | Planned | Defines typed InputMode semantics and how Pause requests modes after ownership is clear. |
| F28F | Next Implementation Closeout | Planned | Selects the next code phase and writes entry criteria, smoke target and file placement rules. |

## Expected Phase Output

At F28 closeout, the project should have:

```text
1. a corrected roadmap from the current freeze forward;
2. an adapter/module map that separates framework core, Unity surface, optional modules and project assets;
3. a dependency order for Player, InputMode, Pause integration, Camera, Audio, Save and Gameplay;
4. a clear next implementation phase with one first runtime objective;
5. updated docs/indexes so future cuts do not rediscover the same boundary questions.
```

## InputMode Positioning

InputMode remains important, but it is no longer the title of the whole phase.

InputMode belongs after the ownership questions below are answered:

```text
who owns the player object;
who supplies Unity Input System targets;
how UI input remains available during PauseOverlay;
how gameplay command input is stopped without scattering Gate checks;
how future multi-player or slot ownership is represented.
```

The preliminary typed modes remain a useful candidate set:

```text
Gameplay
PauseOverlay
FrontendMenu
InputLocked
```

These names are planning candidates. Unity action-map strings are adapter configuration, not canonical framework identity.

## Gate Role After F28

Gate remains capability/admission language.

Canonical Gate use after F28:

```text
admission diagnostics
lifecycle request guards
hard-lock / exceptional block language
safety layer for stale, foreign or in-flight operations
```

Pause/InputMode should not be implemented by forcing every ordinary input consumer to query Gate.

## NewScripts Reference

NewScripts remains architecture reference only. It can inform the plan but must not be copied directly.

Useful concepts:

```text
SessionActivityPauseToggleInputAdapter
  narrow PauseToggle adapter and duplicate callback dedupe

InputModeService
  typed mode requests and technical action-map application

ADR-0007 Gates, InputModes and Simulation Executors
  Gate and InputMode execute effects; they do not decide lifecycle ownership
```

The framework should preserve the separation, not the old service shape.

## Implementation Freeze During F28

Until F28F selects the next implementation phase, do not add runtime code for:

```text
PlayerInput ownership
InputMode runtime service
action-map switching
Pause-driven gameplay command blocking
Time.timeScale policy
Camera/audio/actor/save gameplay adapters
adapter registries or service locator behavior
```
