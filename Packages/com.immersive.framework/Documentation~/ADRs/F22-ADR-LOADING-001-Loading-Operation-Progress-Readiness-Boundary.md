# F22-ADR-LOADING-001 - Loading Operation Progress Readiness Boundary

Status: Accepted / F22D Applied / F22E Next  
Phase: F22 - Loading Operation / Progress / Readiness Boundary  
Type: Framework Core + Loading Module Boundary  
Last updated: 2026-06-26

---

## 1. Context

F18 closed Transition Orchestration as flow orchestration. F19 closed Transition Effects, including fade/curtain adapter boundaries and effect kinds that can represent visual loading-screen requests. F20 closed Pause State/Gate. F21 closed Save / Snapshot / Preferences / Progression Save Foundation.

Loading now needs its own canonical boundary because operation/progress/readiness reporting is not the same responsibility as visual transition effects, SceneLifecycle execution, Route Scene Composition or Transition Orchestration. Without this boundary, loading concerns stay distributed across existing systems and become ghost/parallel concepts.

`NewScripts` is reference-only for concepts. F22 does not copy code, assets, configs, ProjectSettings or runtime architecture from the old project.

---

## 2. Decision

F22 is:

```text
Loading Operation / Progress / Readiness Boundary
```

Loading owns operation, step, weighted progress and readiness observation contracts.

Loading is not:

```text
fade
curtain
loading screen prefab
SceneLifecycle replacement
Transition replacement
TransitionEffect replacement
UI visual requirement
```

Loading visual belongs to an adapter boundary. F22E may define the adapter boundary, but the core Loading contract must remain visual-agnostic.

---

## 3. Canonical boundary

Loading may define:

```text
loading operation identity
loading operation status
loading step identity
loading step status
weighted progress primitives
progress aggregation result
readiness wait/observation contracts
loading result records
loading issue/failure records
diagnostic facts
observation adapter contracts
```

Loading must not own:

```text
SceneLifecycle execution
Route lifecycle execution
Activity lifecycle execution
Transition orchestration execution
Transition visual effects
fade/curtain adapters
loading screen prefab
concrete UI show/hide
save backend persistence
gameplay readiness mutation
```

SceneLifecycle / Transition integration in F22 is observation only unless a later ADR explicitly changes that boundary.

---

## 4. Existing loading-like concepts audit

F22A records the following reconciliation to avoid orphan or ghost loading concepts:

| Existing area | Existing concept | F22 decision |
|---|---|---|
| `SceneLifecycle` | scene load/unload execution | Remains owner of actual Unity scene operations. F22 may observe progress/result; it must not replace execution. |
| `Route Scene Composition` | route scene composition and release results | Remains route content lifecycle evidence. F22 may aggregate/report progress; it must not become route lifecycle owner. |
| `Transition` | operation/step/snapshot diagnostics | Remains flow orchestration. F22 may observe transition-related loading work; it must not redefine Transition. |
| `TransitionEffects` | `LoadingScreen` / `LoadingProgress` effect kinds | Remain visual/effect adapter vocabulary. They are not F22 `LoadingOperation` or progress contracts. |
| `Pause` | pause snapshot/gate state | Not Loading. Pause content/overlay/input remains F23. |
| `ProgressionSave` | load/save request path | Not Loading. Save load is persistence, not loading operation progress. |

F22B must introduce a single canonical namespace for Loading primitives. Any future code that reports loading operation/progress/readiness should use that namespace rather than adding new ad hoc `Loading*` shapes under SceneLifecycle, TransitionEffects, Pause or Save.

---

## 5. F22 implementation plan

| Cut | Status | Objective |
|---|---|---|
| F22A | `APPLIED / DOCS ONLY` | Loading Architecture ADR Plan. |
| F22B | `APPLIED / PRIMITIVES` | Loading Operation / Step / Weighted Progress Primitives. |
| F22C | `APPLIED / AGGREGATION SMOKE` | Loading Progress Aggregation Smoke. |
| F22D | `APPLIED / OBSERVATION ADAPTER` | SceneLifecycle / Transition Loading Observation Adapter. |
| F22E | `NEXT / PLANNED` | Loading Screen Adapter Boundary. |
| F22F | `PLANNED` | Closure + Usage Guide. |

---

## 6. F22A result

F22A is documentation-only. It accepts the Loading architecture plan, updates the roadmap and ADR index, and records the reconciliation with existing loading-like concepts.

F22A does not implement:

```text
runtime code
asmdef changes
LoadingOperation primitives
progress aggregator
smoke
fade
curtain
loading screen prefab
UI
scene object
prefab
ScriptableObject
SceneLifecycle replacement
Transition replacement
backend
PlayerPrefs
JSON
```

---


## 7. F22B result

F22B adds the canonical passive Loading primitive namespace:

```text
Runtime/Loading/
Immersive.Framework.Loading
```

F22B introduces:

```text
LoadingOperationId
LoadingStepId
LoadingOperationStatus
LoadingStepStatus
LoadingProgress
LoadingStepWeight
LoadingWeightedProgress
LoadingStep
LoadingOperation
```

F22B also adds:

```text
FrameworkIdentityDomain.Loading
```

These primitives are passive records for operation identity, step identity, status, normalized progress and weighted progress. They are not an executor, scheduler, SceneLifecycle wrapper, Transition replacement, TransitionEffect, loading screen prefab, UI contract or save/load backend.

F22B does not implement:

```text
progress aggregation runtime
smoke
SceneLifecycle observation adapter
Transition observation adapter
readiness wait contracts
LoadingResult / LoadingFailure records
loading screen adapter
fade
curtain
UI
scene object
prefab
ScriptableObject
backend
PlayerPrefs
JSON
asmdef changes
```

F22C adds pure Loading progress aggregation and a synthetic QA smoke.

F22C introduces:

```text
LoadingProgressAggregationStatus
LoadingProgressAggregationResult
LoadingProgressAggregator
Run Loading Progress Aggregation Smoke
```

The aggregation combines passive `LoadingStep` records using `LoadingStepWeight` and `LoadingProgress`. It validates running, completed-with-skipped, failed and no-step cases. It does not execute SceneLifecycle, Transition, TransitionEffects, readiness mutation or UI.

## 8. F22D result

F22D adds the first Loading observation adapter:

```text
Runtime/Loading/LoadingObservationAdapter.cs
Runtime/Diagnostics/LoadingObservationQaSmokeRunner.cs
```

The adapter maps existing diagnostics into canonical Loading records:

```text
SceneLifecycleLoadResult -> LoadingStep
SceneLifecycleUnloadResult -> LoadingStep
TransitionResult / TransitionStep -> LoadingStep + LoadingProgressAggregationResult
LoadingProgressAggregationResult -> LoadingOperation
```

This is observation-only. F22D does not execute scene loading/unloading, replace `SceneLifecycle`, replace `Transition`, run `TransitionEffects`, mutate readiness, create UI, show fade/curtain, instantiate a loading screen prefab, create scene objects, create ScriptableObjects, use backend/PlayerPrefs/JSON or alter asmdefs.

F22D adds `Run Loading Observation Adapter Smoke` under QA Canvas `Show Loading diagnostics`. The smoke validates SceneLifecycle success/skip/failure mapping, Transition success/failure mapping and the canonical boundary.

## 9. Consequences

Loading can report progress/readiness without becoming a visual effect system.

F19 remains the owner of Transition Effects and fade/curtain adapter boundaries. Any `LoadingScreen` or `LoadingProgress` vocabulary there remains visual/effect-facing and does not become the canonical Loading model.

SceneLifecycle remains the owner of scene lifecycle execution. Loading observes lifecycle/transition progress instead of replacing that owner.

Pause Content / Overlay / Input stays in F23. Gameplay Adapter Foundation stays in F24.
