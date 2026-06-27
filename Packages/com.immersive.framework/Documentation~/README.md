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
| F25 | Adapter Module Foundation | Deferred after F24 |

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
| F24E | Pause Overlay Unity Build |
| F24F | Save Moment Authoring Boundary |
| F24G | Preferences Authoring Boundary |
| F24H | Closure + Usage Guide |

F24B must be the first technical cut. `Transition` already exists as framework language, but `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain, loading or pause visuals are built.

## F25 Boundary

F25 is Adapter Module Foundation. It is broader than gameplay-only work and can later cover gameplay, camera, audio, input, advanced save authoring, pooling/runtime spawned objects, actor/player/NPC, inventory, combat, projectile and damage adapters.

F25 must consume framework contracts and F24 Unity build surfaces. It must not create a parallel lifecycle pipeline or move optional subsystem behavior into framework core.
