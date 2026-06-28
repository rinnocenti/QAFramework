# Immersive Framework Roadmap

This is the canonical roadmap for `com.immersive.framework`.

The roadmap is organized around three implementation tracks:

1. Framework Core / Contracts
2. Unity Build Surface / Lifecycle Wiring
3. Adapter Modules / Gameplay & Subsystems

## 1. Rule of Reading

Read the plan from stable core contracts to Unity build surfaces, then to adapter modules.

Framework contracts define lifecycle language, identity, state, diagnostics and ownership boundaries. Unity build surfaces prove those contracts through minimal real Unity wiring. Adapter modules consume the proven contracts and surfaces without changing framework ownership.

Do not read adapter module work backward into earlier phases. F23 does not build visuals. F24 does not build gameplay modules. F25-F26 stabilize Activity scene operation/loading progress. F27 validates Pause surface/input evidence. F28 reorganizes the roadmap before module implementation resumes.

## 2. Closed Baseline

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
| F24 | Unity Build Surface / Lifecycle Wiring | Closed | Transition, loading and Pause-facing Unity surfaces validated as framework-owned build evidence. |
| F25 | Activity Content Scene Composition / Operation Reset | Closed | Activity content, operation planning, scene loading/release and ledger ownership stabilized. |
| F26 | Activity Discovery and Loading Progress | Closed | Activity discovery and loading progress closed through F26F. |
| F27 | Pause UIGlobal Surface and Input Wiring | Frozen after F27D | Pause UIGlobal surface and narrow PauseToggle input path validated; F27E cancelled. |

## 3. Current Phase

F28 is the current planning phase.

The project is back at the F27D freeze. The next step is not another runtime patch. The next step is a positive roadmap correction that explains what the remaining implementation tracks are and in what order they should be built.

F27E is cancelled because ordinary input consumers should not each query Gate as the primary Pause/Input implementation. That cancellation exposes a larger missing chain:

```text
adapter/module ownership
  -> Player/Actor ownership
  -> Unity Input target ownership
  -> typed InputMode
  -> Pause requests InputMode
  -> later Camera/Audio/Save/RuntimeSpawned/Gameplay adapters
```

## 4. Phase Map From the Freeze Forward

### F28 — Roadmap Reconciliation and Adapter Module Spine

F28 is documentation-first. It creates the dependency and ownership map for the next implementation phases.

| Cut | Name | Status | Output |
|---|---|---|---|
| F28A | Frozen Baseline Reconciliation | Closed / docs-only | Authoritative reading of package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Next | Ordered graph for remaining product-completion tracks. |
| F28C | Adapter Module Taxonomy | Planned | Module families, owner kind, placement rule and dependency category. |
| F28D | Player / Actor / Input Ownership Plan | Planned | Player object ownership, `PlayerInput` target ownership and first input target proof. |
| F28E | InputMode and Pause Integration Plan | Planned | Typed InputMode semantics and Pause-driven mode requests after ownership is clear. |
| F28F | Next Implementation Closeout | Planned | Next code phase, entry criteria, smoke target and file placement rules. |

F28 output is a clean plan, not runtime contracts.

F28A closure confirms the freeze baseline and source boundary:

```text
F24-F27 = frozen Unity-facing evidence
F27E = cancelled
Assets/ = project-facing operational source
Packages/com.immersive.framework/ = framework source
```

F28B now owns the dependency graph; it still does not create runtime contracts.

### F29 — Adapter Module Foundation

F29 is the likely first implementation phase after F28, only if F28F selects it.

Expected purpose:

```text
create minimal adapter module vocabulary
place adapter module contracts in the correct assembly/package boundary
prove one adapter family without pulling product behavior into framework core
```

F29 should not start until F28 defines owner kinds, placement rules and dependency categories.

### F30+ — Product-Facing Module Tracks

The later tracks should be opened incrementally instead of as a single large subsystem pass.

| Track | First dependency | Expected direction |
|---|---|---|
| Player / Actor | Adapter module foundation | Decide player object lifetime, player participant binding and actor adapter placement. |
| Unity Input | Player/Input target ownership | Adapt typed input language to concrete Unity Input System targets. |
| InputMode / Pause Integration | Unity Input target path | Let Pause request `PauseOverlay` / `Gameplay` modes without consumer-side Gate scattering. |
| Camera | Content Anchor binding + player/actor targets where needed | Build camera adapter as a consumer, not framework core authority. |
| Audio | Route/Activity/Pause lifecycle evidence | Build audio as lifecycle consumer with project policy outside core. |
| Save / Progression | Snapshot/progression ports | Add replaceable initial backend adapter without making backend the contract. |
| Runtime Spawned / Pooling | Runtime handle/root/release policy | Materialize prefab/pool instances through framework runtime ownership. |
| Gameplay Capabilities | Player/Actor/RuntimeSpawned | Add inventory/combat/projectile/damage as optional modules. |

## 5. Canonical Tracks

| Track | Owns | Examples | Exclusions |
|---|---|---|---|
| Framework Core / Contracts | Pure framework contracts, identifiers, states, diagnostics, policies and request/result boundaries. | Route, Activity, Gate, Transition, Loading, Pause, Snapshot, Preferences, Progression Save. | Concrete UI, prefabs, scenes, gameplay modules, backend-specific canonical contracts. |
| Unity Build Surface / Lifecycle Wiring | Minimal Unity surfaces and lifecycle wiring that prove framework contracts. | Transition integration, curtain, loading screen, pause overlay, QA scene evidence. | Product gameplay, broad subsystem adapters, full UI systems. |
| Adapter Modules / Gameplay & Subsystems | Optional modules that adapt product/gameplay systems to framework contracts. | Player, actor, camera, audio, input, save backends, inventory, combat, projectile, damage, spawned objects. | Core contract ownership, route/activity lifecycle, hidden global pipelines. |
| Project Assets | Product-specific prefabs, scenes, configuration, UI art and game design content. | Player prefab, product pause menu, concrete camera rig, concrete audio bank. | Canonical framework ownership or reusable framework contracts. |
| External Packages | Unity official modules, optional packages, third-party tools and premium assets. | Unity Input System, Cinemachine, Addressables, future premium save backend. | Hard-coded local paths or mandatory Asset Store imports in canonical setup. |

## 6. Anti-Regression Rules

- Snapshot does not know a backend.
- Preferences do not use progression slots.
- Progression Save uses a replaceable backend port.
- JSON persistence is a future initial adapter, not the canonical contract.
- A future premium backend must swap behind the same Progression Save port.
- Loading is not fade, curtain, loading screen prefab or `SceneLifecycle` replacement.
- Loading visual remains a Unity build surface or adapter, not the Loading contract.
- Pause F23 remains intent/requirement language.
- F24-F27 surfaces are evidence, not gameplay modules.
- F25 is Activity scene operation history, not the adapter module foundation phase.
- Adapter modules do not own Route/Activity lifecycle.
- Adapter modules do not redefine Gate, Transition, Loading, Pause, Save, Snapshot, Preferences or Progression Save.
- No framework identity may be fabricated from Unity names or paths.
- No module should create service locator behavior to avoid an ownership decision.

## 7. F27 Pause UIGlobal / Input / Gate Reframe

`F27A` validates the Pause UIGlobal surface. `F27B` validates the narrow Unity Input System `PauseToggle` adapter. `F27C` audits the Gate/Input boundary and accepts that Gate is capability/admission language, not a component blocker. `F27D` applies the diagnostic/runtime vocabulary correction by using `Input/InputAcceptance` and `Interaction/InteractionAcceptance` blockers instead of broad gameplay/component blockers.

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

## 8. F28 Roadmap Correction

F28 converts the freeze into a progressive plan.

The phase must answer:

```text
what remains to complete the framework/product bridge;
which tracks are core, Unity surface, adapter module, project asset or external package;
which track must be implemented first;
which ownership decision blocks InputMode;
which ownership decision blocks Camera, Audio, Save, RuntimeSpawned and Gameplay modules;
what the first post-F28 code cut proves.
```

F28 is complete only when the next implementation phase has a clear positive objective, file placement rule and smoke target.
