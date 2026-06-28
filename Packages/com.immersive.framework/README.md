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
| F25 | Activity Content Scene Composition | Current after F24 visual policy |
| F26 | Activity Scene Discovery Integration | Current after F25 consolidation |

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

F25 currently stabilizes Activity Content Scene Composition before broader adapter module work resumes. Adapter module foundation remains deferred until Activity scene operation ownership is stable.

`F25R - Activity Scene Operation Architecture Reset` is the documentary reset for Activity scene loading/release. `F25I1` corrects its early visual-mode constraint: `Seamless + Activity scene side-effect` is valid, but must never auto-open LoadingSurface or silently upgrade to another visual mode.

F26A integrates Activity-owned additive scenes tracked by `ActivitySceneLedger` into Activity content discovery while keeping Route content discovery separate. F26A1 clarifies diagnostics: local Activity adapters/handles are reported separately from explicit Activity content participants.

F26B introduces the internal loading progress contract and logging fields without changing LoadingSurface presentation or wiring progress to `SceneManager.LoadSceneAsync` yet. The contract is diagnostic-only for now, and the visual loading path remains unchanged.

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
