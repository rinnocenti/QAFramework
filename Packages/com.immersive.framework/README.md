# Immersive Framework

`com.immersive.framework` is the framework package for Immersive Framework 1.0.

The package is organized as framework contracts first, Unity build surfaces second, and adapter modules last. Old `NewScripts` material remains reference-only and must not be copied into this package.

## Canonical Navigation

- [Documentation README](Documentation~/README.md)
- [Roadmap](Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md)
- [ADR index](Documentation~/ADRs/ADR-INDEX.md)
- [Guides](Documentation~/Guides)

## Current Roadmap Order

| Phase | Name | Status |
|---|---|---|
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed |
| F24 | Unity Build Surface / Lifecycle Wiring | Current |
| F25 | Adapter Module Foundation | Deferred after F24 |

## Tracks

| Track | Owns | Does not own |
|---|---|---|
| Framework Core / Contracts | Pure framework contracts, identifiers, state, diagnostics, boundaries and request/result language. | Unity GameObjects, prefabs, scenes, concrete UI, gameplay modules or backend-specific persistence contracts. |
| Unity Build Surface / Lifecycle Wiring | Minimal Unity surfaces that prove existing framework contracts through real lifecycle wiring. | Product gameplay, broad adapter modules or full UI systems. |
| Adapter Modules / Gameplay & Subsystems | Optional adapters for gameplay, camera, audio, input, advanced save authoring, pooling/runtime spawned objects, actors, inventory, combat, projectiles and damage. | Core ownership, route/activity lifecycle, canonical framework contracts or parallel framework pipelines. |

## Current Contract Boundaries

F23 is closed as intent/requirement-only. Its canonical Pause contracts are:

- `PauseContentRequirement`
- `PausePresentationIntent`
- `PauseInputSignal`
- `PauseInputIntent`

F23 does not promise overlay adapters, Content Anchor binding execution, `RuntimeContentAnchorBinding`, Input System wiring, Canvas, prefabs, scene objects, ScriptableObjects, `Time.timeScale` policy or gameplay adapters.

F24 starts with lifecycle wiring before visual loading/pause surfaces. The first technical cut is `F24B - Transition <-> GameFlow Runtime Integration`, because `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain or loading visuals become meaningful.

F25 is broader than gameplay. It is the adapter module foundation phase and must not mix optional subsystem adapters back into framework core.

## Anti-Regression Rules

- Snapshot does not know a backend.
- Preferences do not use progression slots.
- Progression Save uses a replaceable backend port.
- Future JSON persistence is an initial adapter, not the canonical contract.
- Future premium persistence must swap behind the same port.
- Loading is not fade, curtain, loading screen prefab or `SceneLifecycle` replacement.
- Loading visual belongs to a later adapter/build surface.
- Framework contracts must not depend on Unity object names, paths or hidden global state.
- No implementation, asmdef change or Unity asset belongs in an ADR-only cut.
