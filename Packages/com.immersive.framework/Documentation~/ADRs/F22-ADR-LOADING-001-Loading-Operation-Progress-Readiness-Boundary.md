# F22-ADR-LOADING-001 - Loading Operation Progress Readiness Boundary

Status: Accepted / F22A Planned  
Phase: F22 - Loading Operation / Progress / Readiness Boundary  
Type: Framework Core + Loading Module Boundary  
Last updated: 2026-06-26

---

## 1. Context

F18 closed Transition Orchestration as flow orchestration. F19 closed Transition Effects, including fade/curtain adapter boundaries. F20 closed Pause State/Gate. F21 opens Save before Pause visual/gameplay.

Loading needs a separate boundary because progress/readiness reporting is not the same responsibility as visual transition effects or SceneLifecycle execution.

NewScripts is reference-only for concepts. F22 does not copy code, assets, configs or runtime architecture from the old project.

---

## 2. Decision

F22 is:

```text
Loading Operation / Progress / Readiness Boundary
```

Loading owns operation/progress/readiness contracts.

Loading is not:

```text
fade
curtain
loading screen prefab
SceneLifecycle replacement
```

Loading visual belongs to a later adapter boundary.

---

## 3. Boundary

Loading may define:

```text
loading operation identity
loading operation status
loading step identity
weighted progress primitives
progress aggregation result
readiness observation contracts
diagnostic facts
observation adapter contracts
```

Loading must not own:

```text
SceneLifecycle execution
Route lifecycle execution
Activity lifecycle execution
Transition visual effects
fade/curtain adapters
loading screen prefab
UI concrete show/hide
save backend persistence
gameplay readiness mutation
```

SceneLifecycle / Transition integration in F22 is observation only unless a later ADR explicitly changes that boundary.

---

## 4. F22 Plan

| Cut | Status | Objective |
|---|---|---|
| F22A | `PLANNED / ADR PLAN` | Loading Architecture ADR Plan. |
| F22B | `PLANNED` | Loading Operation / Step / Weighted Progress Primitives. |
| F22C | `PLANNED` | Loading Progress Aggregation Smoke. |
| F22D | `PLANNED` | SceneLifecycle / Transition Loading Observation Adapter. |
| F22E | `PLANNED` | Loading Screen Adapter Boundary. |
| F22F | `PLANNED` | Closure + Usage Guide. |

---

## 5. Excluded Now

F22A does not implement:

```text
runtime code
fade
curtain
loading screen prefab
UI
scene object
ScriptableObject
asmdef changes
SceneLifecycle replacement
Transition replacement
backend
PlayerPrefs
JSON
```

---

## 6. Consequences

Loading can report progress/readiness without becoming a visual effect system.

F19 remains the owner of Transition Effects and fade/curtain adapter boundaries.

SceneLifecycle remains the owner of scene lifecycle execution. Loading observes lifecycle/transition progress instead of replacing that owner.

Pause Content / Overlay / Input stays in F23. Gameplay Adapter Foundation stays in F24.

