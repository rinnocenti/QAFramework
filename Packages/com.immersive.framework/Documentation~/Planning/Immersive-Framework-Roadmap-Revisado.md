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

F27 is frozen after F27D. F24, F25 and F26 are closed; the F26 loading progress thread is closed through F26F. F27 validated the deferred Pause UIGlobal surface and narrow PauseToggle input path, then stopped before broader InputMode / PlayerInput / adapter ownership work.

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

Historical note: F24 opened Unity Build Surface / Lifecycle Wiring. Its transition/loading surface work is now validated and later F25/F26 cuts build on it.

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


### F26 - Activity Scene Discovery and Loading Progress

| Cut | Name | Status |
|---|---|---|
| F26A | Activity Scene Discovery Integration | Closed / PASS |
| F26A1 | Activity Content Execution Diagnostics Clarification | Closed / PASS |
| F26B | Loading Progress Contract | Closed / PASS |
| F26C | Loading Surface Progress Bar Receiver | Closed / PASS |
| F26D | Determinate Loading Progress Source | Closed / PASS |
| F26E | Aggregated Loading Progress | Closed / PASS |
| F26F | Loading Progress Polish / Documentation Closeout | Closed / PASS |

Loading progress is now considered closed until a new concrete loading source appears, such as Addressables, asset bundles, remote content or long-running non-scene operations.



### F27 - Pause UIGlobal Surface and Input Wiring

| Cut | Name | Status |
|---|---|---|
| F27A | Pause UIGlobal Surface Baseline | Closed / PASS |
| F27B | Pause Input Signal Wiring | Closed / PASS |
| F27C | Gate / Input Capability Audit | Closed / Audit PASS |
| F27D | Pause Capability Gate Reframe | Closed / PASS |
| F27E | Input Consumers Respect Gate | Cancelled / do not apply |

F27 implemented the deferred Pause Unity surface goal and narrow PauseToggle input binding. F27 is now frozen because the next step requires the broader InputMode and adapter boundary to be planned first.

F27E was cancelled because ordinary input consumers should not each become responsible for querying Gate just to make Pause work.

### F28 - InputMode and Adapter Boundary Reorganization

| Cut | Name | Status |
|---|---|---|
| F28A | Input / Adapter Audit Matrix | Planned |
| F28B | InputMode Contract Boundary | Planned |
| F28C | Unity Input Adapter Ownership Plan | Planned |
| F28D | QA InputMode Surface / Manual Target Proof | Planned / conditional |
| F28E | Pause Drives InputMode | Planned / after ownership decision |
| F28F | InputMode Closeout Guide | Planned |

F28 must decide PlayerInput ownership, typed InputMode semantics and adapter placement before Pause drives action-map behavior or TimeScale policy.

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


## F27 Pause UIGlobal / Input / Gate Reframe

`F27A` validates the Pause UIGlobal surface. `F27B` validates the narrow Unity Input System `PauseToggle` adapter. `F27C` audits the Gate/Input boundary and accepts the correction that Gate is capability/admission language, not a component blocker. `F27D` applies that diagnostic correction in runtime by changing Pause-derived blockers from broad gameplay language to `Input/InputAcceptance` and `Interaction/InteractionAcceptance`.

F27 is frozen after F27D. `F27E - Input Consumers Respect Gate` is cancelled and must not be applied.

Corrected F27 rule:

```text
Pause produces state and may produce blockers.
Gate remains passive admission / hard-lock / diagnostics.
InputMode must be planned before action-map behavior.
Gameplay components are not paused directly by Gate.
Ordinary input consumers should not each query Gate just to implement Pause.
TimeScale freeze policy is separate and deferred.
```

See:

```text
ADRs/F27-ADR-GATE-INPUT-001-Capability-Gate-Boundary.md
ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md
Assets/_Documentation/Notes/F27C-Gate-Input-Capability-Audit.md
Assets/_Documentation/Notes/F27D-Pause-Capability-Gate-Reframe.md
Assets/_Documentation/Notes/F27E-CANCELLED-Input-Consumers-Gate-Replan.md
Assets/_Documentation/Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md
```

## F28 InputMode and Adapter Boundary

F28 reorganizes the future input/adapter track before more runtime. It starts with an audit matrix and must answer who supplies concrete `PlayerInput` targets, where Unity Input System adapters live and how PauseOverlay keeps UI input alive while gameplay input is unavailable.
