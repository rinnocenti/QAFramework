# F28 Plan — Roadmap Reconciliation and Adapter Module Spine

## Status

Active / F28A-F28E closed / documentation-first / no runtime changes

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
| F28A | Frozen Baseline Reconciliation | Closed / docs-only | One authoritative reading of the current frozen baseline, including package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Closed / docs-only | Ordered dependency graph for the remaining product-completion tracks. Identifies what must precede Player, InputMode, Pause integration, Camera, Audio, Save, RuntimeSpawned and Gameplay. |
| F28C | Adapter Module Taxonomy | Closed / docs-only | Defines module families, owner kind, placement rule and dependency category. This is still documentation/plan, not runtime registration. |
| F28D | Player / Actor / Input Ownership Plan | Closed / docs-only | Decides the ownership path for project player assets, Unity `PlayerInput` targets, player/actor adapter placement and the first input target proof. |
| F28E | InputMode and Pause Integration Plan | Closed / docs-only | Defines typed InputMode semantics and how Pause requests modes after ownership is clear. |
| F28F | Next Implementation Closeout | Next | Selects the next code phase and writes entry criteria, smoke target and file placement rules. |

## F28A Closure Result

F28A closes the baseline reading and confirms the active source boundary:

```text
Assets/ = project-facing operational source
Packages/com.immersive.framework/ = framework contracts/runtime/source
```

F24-F27 remain frozen evidence. F27E remains cancelled. Runtime implementation stays frozen until F28F selects one concrete next phase.

Reference note:

```text
Assets/_Documentation/Notes/F28A-Frozen-Baseline-Reconciliation.md
```

## F28B Entry Criteria

F28B starts from the F28A baseline and must stay documentation/planning-only.

F28B may produce:

```text
ordered dependency graph
blocker map per module family
first implementation candidates
stop conditions for premature consumers
```

F28B must not create runtime code, adapter registries, PlayerInput ownership, InputMode services, action-map switching, QA buttons or new asmdefs.

## F28B Closure Result

F28B closes the completion dependency map.

Canonical dependency order:

```text
F28A frozen baseline
  -> F28B dependency map
  -> F28C adapter module taxonomy
  -> F28D Player / Actor / Unity Input ownership
  -> F28E InputMode / Pause integration
  -> F28F next implementation selection
```

Family-level blockers:

| Family | Must come before implementation |
|---|---|
| Player / Actor | RuntimeContent ownership, object entry/reset foundations and adapter placement rules. |
| Unity Input | Player target ownership and global UI input target ownership. |
| InputMode | Unity Input target path and typed mode semantics. |
| Pause Integration | InputMode owner and target split. |
| Camera | Content Anchor binding, RuntimeContent release ownership and optional player/actor targets. |
| Audio | Lifecycle consumer boundary and project audio policy split. |
| Save / Progression | Snapshot/progression ports and backend placement rule. |
| RuntimeSpawned / Pooling | Runtime roots, handles, release policy and pooling package boundary. |
| Gameplay Capabilities | Player/Actor and RuntimeSpawned foundations. |

Reference note:

```text
Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md
```

## F28C Closure Result

F28C closes the adapter module taxonomy.

Every future adapter/module cut must declare:

```text
Family
Owner kind
Dependency category
Placement
Evidence surface
First proof
Blocked by
Must not touch
```

Accepted owner kinds:

| Owner kind | Canonical placement |
|---|---|
| Framework Core | `Packages/com.immersive.framework` |
| Framework Unity Adapter | `Packages/com.immersive.framework` or a separate module if optional. |
| Optional Immersive Package | `Packages/com.immersive.<module>` |
| Project Integration | `Assets/_Project` |
| QA Evidence | `Assets/ImmersiveFrameworkQA` |
| External Tool Boundary | `Assets/_External` or a declared package dependency. |
| Sandbox Experiment | `Assets/_Sandbox` |

Accepted dependency categories:

```text
Framework Core
Official Unity Package
Optional Immersive Package
External Tool
Project Asset / Config
Personal Asset
QA Fixture
```

Reference note:

```text
Assets/_Documentation/Notes/F28C-Adapter-Module-Taxonomy.md
```

## F28D Entry Criteria

F28D starts from the F28C taxonomy and focuses only on Player / Actor / Unity Input ownership.

F28D must answer:

```text
who owns the player object lifetime;
where Unity Input System targets live;
how UI input remains available during pause;
how gameplay commands are stopped without scattered Gate checks;
how player/actor participation relates to Activity lifecycle;
which first proof or smoke validates the ownership path.
```

## F28D Closure Result

F28D closes the Player / Actor / Unity Input ownership plan.

Accepted ownership split:

| Concern | Owner |
|---|---|
| Concrete player prefab, controller, visual and `InputActionAsset` | Project Integration / QA Fixture first |
| Unity `PlayerInput` or equivalent input target | Unity Input Adapter proof first; project-owned asset later |
| Typed input language and InputMode names | Framework Core |
| Unity action-map names | Adapter/project configuration |
| Pause mode request | Pause Integration, downstream of InputMode |
| Runtime-spawned player actor lifetime | Deferred until RuntimeContentHandle/runtime root/release policy exist |

Accepted first future proof:

```text
QA-authored Unity input target proof
```

That proof validates explicit target ownership and diagnostics for missing/duplicate targets. It must not spawn actors, implement full InputMode, add camera/movement/save ownership or create a gameplay module.

Reference note:

```text
Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md
```

## F28E Entry Criteria

F28E starts from the F28D ownership split and focuses only on typed InputMode and Pause integration semantics.

F28E must answer:

```text
which InputMode states exist first;
which transitions are legal;
how Pause requests Gameplay/PauseOverlay mode changes;
how UI input remains available while gameplay commands stop;
what the next implementation closeout must prove.
```

F28E must not create runtime code, action-map switching, PlayerInput components, player actor lifecycle, camera follow behavior, movement systems or save/gameplay adapters.

## F28E Closure Result

F28E closes the InputMode and Pause integration plan.

Accepted first mode vocabulary:

| Mode | Meaning |
|---|---|
| `Gameplay` | Gameplay command posture. |
| `PauseOverlay` | Pause UI posture over an existing gameplay route/activity. |
| `FrontendMenu` | Reserved future non-gameplay menu posture. |
| `InputLocked` | Reserved future hard suppression posture for transition/loading/exceptional lock states. |

Accepted Pause integration rule:

```text
Pause may request InputMode changes after InputMode exists.
Pause does not own PlayerInput, action-map strings, player/actor lifecycle, movement, Time.timeScale, camera, audio, save or gameplay adapters.
UI/Pause input remains available during PauseOverlay.
Gameplay command input stops driving gameplay during PauseOverlay.
```

F28F is now the next cut and must choose one code phase with entry criteria, file placement and smoke target. The preferred first code direction remains a QA-authored Unity input target proof, unless F28F explicitly chooses a smaller core InputMode identity/result model first.

Reference note:

```text
Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md
```

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
