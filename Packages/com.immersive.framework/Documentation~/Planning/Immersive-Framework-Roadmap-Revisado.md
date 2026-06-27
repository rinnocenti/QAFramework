# Immersive Framework Roadmap

This is the canonical roadmap for `com.immersive.framework`.

The roadmap is intentionally organized around three tracks:

1. Framework Core / Contracts
2. Unity Build Surface / Lifecycle Wiring
3. Adapter Modules / Gameplay & Subsystems

## 1. Rule of Reading

Read the plan from core contracts to Unity build surfaces to adapter modules.

Framework contracts define stable language and boundaries. Unity build surfaces prove those contracts through minimal real Unity wiring. Adapter modules consume the proven contracts and surfaces without changing framework ownership.

Do not read F25 adapter work back into F23 or F24. F23 does not build visuals. F24 does not build gameplay modules. F25 does not redefine core contracts.

## 2. Closed Phases

| Phase | Name | Status | Result |
|---|---|---|---|
| F00-F10 | Baseline, identities, route/activity, anchors, runtime materialization, input, pause and snapshot foundations | Closed | Core ownership language established. |
| F11-F16 | Reset, object entry, Unity reset adapters, player participant entry | Closed | Reset and participant foundations established without gameplay adapter ownership. |
| F17 | Gate Boundary / Advanced Consumers Boundary | Closed | Gate and advanced consumer boundaries established. |
| F18 | Transition Orchestration | Closed | Transition language and orchestration contracts established. |
| F19 | Transition Effects Boundary | Closed | Transition effect adapter boundary established. |
| F20 | Pause State and Gate | Closed | Logical Pause state and Gate relationship established. |
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed | Save tracks split into Snapshot, Preferences and Progression Save boundaries. |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed | Loading operation/progress/readiness language established without visual loading implementation. |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed | Pause content, presentation and input intent contracts established without Unity build surface. |

## 3. Current Phase

F23 is closed.

F23 is intent/requirement-only and owns these canonical Pause contracts:

- `PauseContentRequirement`
- `PausePresentationIntent`
- `PauseInputSignal`
- `PauseInputIntent`

F23 does not promise or create:

- overlay adapter execution
- Content Anchor binding execution
- `RuntimeContentAnchorBinding`
- Input System wiring
- Canvas
- prefab
- scene object
- ScriptableObject
- `Time.timeScale` policy
- gameplay adapter

The next phase is F24 - Unity Build Surface / Lifecycle Wiring.

F24B must be the first technical cut because `Transition` exists as language, but `RouteRequestTrigger` / `GameFlow` must pass through a real `TransitionPlan` before curtain, loading screen or pause overlay surfaces are built.

## 4. Next Phases

### F24 - Unity Build Surface / Lifecycle Wiring

| Cut | Name | Objective |
|---|---|---|
| F24A | Unity Build / Lifecycle Wiring ADR Plan | Lock the F24 boundary and ordering. |
| F24B | Transition <-> GameFlow Runtime Integration | Route/GameFlow requests pass through real `TransitionPlan` execution. |
| F24C | Transition Curtain Unity Build | Build the minimal Unity curtain surface after lifecycle wiring is real. |
| F24D | Loading Screen Unity Adapter Build | Build the minimal loading screen adapter surface after progress/readiness contracts exist. |
| F24E | Pause Overlay Unity Build | Build the minimal pause overlay Unity surface from F23 intent contracts. |
| F24F | Save Moment Authoring Boundary | Define minimal Unity authoring for save moments without gameplay payload ownership. |
| F24G | Preferences Authoring Boundary | Define minimal Unity authoring for preferences without using progression slots. |
| F24H | Closure + Usage Guide | Close the phase and document usage. |

F24 is not gameplay. It is the framework-owned Unity surface that proves lifecycle wiring.

### F25 - Adapter Module Foundation

F25 starts only after F24.

F25 is broader than gameplay-only work. It defines how optional adapter modules consume framework contracts and Unity build surfaces without moving subsystem behavior into core.

Expected adapter families include:

- gameplay
- camera
- audio
- input
- advanced save authoring
- pooling/runtime spawned objects
- actor/player/NPC
- inventory
- combat
- projectile
- damage

F25 must not create a parallel lifecycle, bypass Gate/Transition/Loading/Pause ownership or redefine Save/Snapshot/Preferences/Progression contracts.

## 5. Tracks

| Track | Owns | Examples | Exclusions |
|---|---|---|---|
| Framework Core / Contracts | Pure framework contracts, identifiers, states, diagnostics, policies and request/result boundaries. | Route, Activity, Gate, Transition, Loading, Pause, Snapshot, Preferences, Progression Save. | Concrete UI, prefabs, scenes, gameplay modules, backend-specific canonical contracts. |
| Unity Build Surface / Lifecycle Wiring | Minimal Unity surfaces and lifecycle wiring that prove framework contracts. | Transition integration, curtain, loading screen adapter build, pause overlay build, save/preference authoring boundary. | Product gameplay, broad subsystem adapters, full UI systems. |
| Adapter Modules / Gameplay & Subsystems | Optional modules that adapt product/gameplay systems to framework contracts. | Player, actor, camera, audio, input, inventory, combat, projectile, damage, spawned objects. | Core contract ownership, route/activity lifecycle, hidden global pipelines. |

## 6. Anti-Regression Rules

- Snapshot does not know a backend.
- Preferences do not use progression slots.
- Progression Save uses a replaceable backend port.
- JSON persistence is a future initial adapter, not the canonical contract.
- A future premium backend must swap behind the same Progression Save port.
- Loading is not fade, curtain, loading screen prefab or `SceneLifecycle` replacement.
- Loading visual remains a later adapter/build surface.
- Pause F23 remains intent/requirement-only.
- F24 must start technical work with Transition <-> GameFlow runtime integration.
- F25 is Adapter Module Foundation, not a gameplay-only rewrite of core.
- No route/activity lifecycle ownership moves into gameplay or adapter modules.
- No framework identity may be fabricated from Unity names or paths.
