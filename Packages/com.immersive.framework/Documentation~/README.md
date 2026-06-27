# Immersive Framework Documentation

This folder contains the canonical documentation for `com.immersive.framework`.

Read the documentation in this order:

1. [Roadmap](Planning/Immersive-Framework-Roadmap-Revisado.md)
2. [ADR index](ADRs/ADR-INDEX.md)
3. [Guides](Guides)

## Canonical Tracks

| Track | Purpose |
|---|---|
| Framework Core / Contracts | Defines pure framework language: contracts, identities, state, diagnostics and boundaries. |
| Unity Build Surface / Lifecycle Wiring | Proves framework contracts through minimal Unity surfaces and lifecycle wiring. |
| Adapter Modules / Gameplay & Subsystems | Adds optional adapters for gameplay and subsystems without moving them into core. |

## Current Phase Map

| Phase | Name | Status |
|---|---|---|
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed |
| F24 | Unity Build Surface / Lifecycle Wiring | Next |
| F25 | Activity Content Scene Composition | Current after F24 visual policy |

## F23 Boundary

F23 is closed as intent/requirement-only. It keeps Pause language in framework core while deferring every concrete Unity surface.

Canonical F23 contracts:

- `PauseContentRequirement`
- `PausePresentationIntent`
- `PauseInputSignal`
- `PauseInputIntent`

F23 does not create or promise overlay adapters, Content Anchor binding execution, `RuntimeContentAnchorBinding`, Input System wiring, Canvas, prefabs, scene objects, ScriptableObjects, `Time.timeScale` policy or gameplay adapters.

## F24 Boundary

F24 is the next phase. It is a Unity Build Surface / Lifecycle Wiring phase, not an adapter module phase.

Planned cuts:

| Cut | Name |
|---|---|
| F24A | Unity Build / Lifecycle Wiring ADR Plan |
| F24B | Transition <-> GameFlow Runtime Integration |
| F24C | Transition Curtain Unity Build |
| F24D | Loading Screen Unity Adapter Build |
| F24E | Canonical UIGlobal Surface / Loading Cleanup |
| F24F | Activity Transition Policy |
| F24G | Save Moment / Preferences Authoring Boundary |
| F24H | Closure + Usage Guide |

F24B must be the first technical cut. `Transition` already exists as framework language, but `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain, loading or pause visuals are built.

Project documentation now splits the UIGlobal work into `F24E1 - Surface/Loading Legacy Cleanup` and `F24E2 - Route/Activity Visual Operation Policy`. See `Assets/_Documentation/Plans/F24-PLAN-Unity-Build-Surface.md` for the project cut list.
`F24E3 - Surface Adapter Inspector Cleanup` keeps the same runtime shape and trims only authoring/Inspector exposure.

## F25 Boundary

F25 opens as Activity Content Scene Composition. It is framework lifecycle/content core, not a gameplay adapter track. Later adapter module work can still cover gameplay, camera, audio, input, advanced save authoring, pooling/runtime spawned objects, actor/player/NPC, inventory, combat, projectile and damage adapters after the Activity content boundary is stable.

F25 must consume F24 Unity build surfaces and must not create a parallel lifecycle pipeline or move optional subsystem behavior into framework core.

`F24F - Activity Transition Policy` adds an Activity-level authoring policy for optional Activity transitions. Route transitions remain mandatory; Activity loading remains skipped until real Activity content/scene loading exists.

`F24F1 - Activity Loading Reserved Finding` marks `FadeWithLoading` as reserved. Activity loading remains `SkippedNoSceneLoad` until an Activity content composition track adds `ActivityContentProfile`, Activity scene composition plan/result, execution and release.

## F25 Activity Content Scene Composition

F25 now opens with Activity content scene composition before broader adapter modules.

`F25A - Activity Content Profile Contract` adds declaration-only Activity content authoring:

- `ActivityContentProfileAsset`
- `ActivityContentSceneEntry`
- `ActivityContentSceneLoadMode`
- `ActivityContentReleasePolicy`
- `ActivityAsset.ActivityContentProfile`

F25A does not load Activity scenes. Activity loading remains `SkippedNoSceneLoad` until Activity scene composition execution is implemented in a later F25 cut.

Project plan: `Assets/_Documentation/Plans/F25-PLAN-Activity-Content-Scene-Composition.md`.


`F25B - Activity Scene Composition Plan/Result` adds side-effect-free Activity scene composition diagnostics. Activity requests can now report planned Activity content scenes, required/optional counts and execution-ready declarations, but Activity scene loading and release remain deferred to later F25 cuts.

`F25C - Activity Scene Composition Execution` loads execution-ready Activity content scenes additively. When a canonical `LoadingSurface` exists, Activity scene composition runs inside the loading window. Progress remains indeterminate and Activity content release is deferred to F25D.

### IF-FW-F25D — Activity Content Release

Activity-owned additive scenes loaded through Activity scene composition are now released on Activity change when their scene entry uses `ReleaseOnActivityChange`.
The release operation runs inside the Activity loading window when a LoadingSurface is available and is reported through `activitySceneRelease*` diagnostics.
`KeepOnActivityChange` is valid only across Activity changes; Route changes always force-release Activity-owned scenes.

### IF-FW-F25D1 — Activity release policy semantics

Activity content release policy is scoped to Activity changes only. `ReleaseOnActivityChange` unloads Activity-owned scenes when the Activity is replaced or cleared. `KeepOnActivityChange` keeps them loaded across Activity changes.

Route changes always force-release Activity-owned scenes, regardless of Activity policy. Route content has no release policy; content that survives Route changes must be modeled as Session content.
