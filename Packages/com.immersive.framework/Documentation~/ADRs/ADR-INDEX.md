# ADR Index

Canonical framework plan:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

ADRs record accepted architectural decisions. They do not replace the operational roadmap and must not redefine phase order.

F11 is closed as `Cycle Reset Foundation`. F12 is closed as `Cycle Reset Integration & Authoring UX`. F13 is closed as `Object Entry Foundation`. F14 is closed as `Local Object Reset Foundation`. F15 is closed as `Unity Reset Adapters minimos`. F16 is closed as `GameObject Active State Reset Adapter`. F17 is closed as `Gate Foundation`. F18 is closed as `Transition Orchestration Foundation`. F19 is closed as `Transition Effects`. F20 is closed as `Pause State and Gate`. F21 is closed for Save / Snapshot / Preferences / Progression Save Foundation. F21A through F21G are applied and F21H closes the phase with a usage guide. F22 is closed for Loading Operation / Progress / Readiness Boundary. F22B is applied for Loading Operation / Step / Weighted Progress Primitives. F22C is applied for Loading Progress Aggregation Smoke. F22D is applied for SceneLifecycle / Transition Loading Observation Adapter. F22E is applied for Loading Screen Adapter Boundary. F22F is applied for Closure + Usage Guide. F22G/F22H close the pre-F23 framework-only Loading readiness/result debt. F23A is applied as Pause Content/Overlay/Input ADR Plan. F23B is applied as Pause Content Anchor Consumer Contracts. F23C is applied as Pause Overlay Adapter Boundary. F23D is applied as Pause Input Boundary Contracts. F24 is Unity Build Surface / Lifecycle Wiring. Gameplay Adapter Foundation moves to F25.

## Accepted ADRs

| Phase | ADR | Status |
|---|---|---|
| F00 | [Baseline Reconciliation](F00-ADR-BL-001-Baseline-Reconciliation.md) | Accepted |
| F00 | [Core vs Consumers](F00-ADR-BL-002-Core-vs-Consumers.md) | Accepted |
| F01 | [Typed Identity Policy](F01-ADR-ID-001-Typed-Identity-Policy.md) | Accepted |
| F01 | [Framework Facts](F01-ADR-DIAG-001-Framework-Facts.md) | Accepted |
| F02 | [Session Scope](F02-ADR-SESSION-001-Session-Scope.md) | Accepted |
| F03 | [Route Baseline](F03-ADR-ROUTE-001-Route-Baseline.md) | Accepted |
| F04 | [Activity Baseline](F04-ADR-ACTIVITY-001-Activity-Baseline.md) | Accepted |
| F05 | [Local Identity and Contribution](F05-ADR-LOCAL-001-Local-Identity-and-Contribution.md) | Accepted |
| F06 | [Route Scene Composition](F06-ADR-SCENE-001-Route-Scene-Composition.md) | Accepted |
| F06 | [Content Release](F06-ADR-RELEASE-001-Content-Release.md) | Accepted |
| F07 | [Content Anchor Declaration](F07-ADR-ANCHOR-001-Content-Anchor-Declaration.md) | Accepted |
| F08 | [Runtime Materialization](F08-ADR-RUNTIME-001-Runtime-Materialization.md) | Accepted |
| F09 | [Content Anchor Binding](F09-ADR-ANCHOR-002-Content-Anchor-Binding.md) | Accepted |
| F10 | [Input Ownership](F10-ADR-INPUT-001-Input-Ownership.md) | Accepted |
| F10 | [Snapshot Model](F10-ADR-SNAPSHOT-001-Snapshot-Model.md) | Accepted / superseded operationally by F21 canonical Snapshot module |
| F10 | [Pause as Consumer](F10-ADR-PAUSE-001-Pause-as-Consumer.md) | Accepted |
| F11 | [Cycle Reset Foundation](F11-ADR-RESET-001-Cycle-Reset-Foundation.md) | Applied through F11G |
| F12 | [Cycle Reset Integration and Authoring UX](F12-ADR-RESET-002-Cycle-Reset-Integration-Authoring-UX.md) | Closed / Applied through F12E |
| F13 | [Object Entry Foundation](F13-ADR-OBJECT-001-Object-Entry-Foundation.md) | Closed / Applied through F13L |
| F14 | [Local Object Reset Foundation](F14-ADR-RESET-003-Local-Object-Reset-Foundation.md) | Closed / Applied through F14H |
| F15 | [Unity Reset Adapters](F15-ADR-RESET-004-Unity-Reset-Adapters.md) | Closed / Applied through F15F |
| F16 | [GameObject Active State Reset Adapter](F16-ADR-RESET-005-GameObject-Active-State-Reset.md) | Closed / Applied through F16 |
| Future | [Player Participant Entry Baseline](F16-ADR-PLAYER-001-Player-Participant-Entry-Baseline.md) | Deferred / Future Contextual Reset |
| F17 | [Gate Boundary](F17-ADR-GATE-001-Gate-Boundary.md) | Accepted / Closed F17E |
| F18 | [Transition Orchestration](F18-ADR-TRANSITION-001-Transition-Orchestration.md) | Accepted / Closed F18F |
| F19 | [Transition Effects Boundary](F19-ADR-TRANSITION-002-Transition-Effects-Boundary.md) | Accepted / Closed F19F |
| F20 | [Pause State and Gate](F20-ADR-PAUSE-002-Pause-State-and-Gate.md) | Accepted / Closed F20F |
| F21 | [Save Snapshot Preferences Progression Boundary](F21-ADR-SAVE-001-Save-Snapshot-Preferences-Progression-Boundary.md) | Accepted / Closed F21H |
| F22 | [Loading Operation Progress Readiness Boundary](F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md) | Closed / F22H QA PASS |
| F23 | [Pause Content Overlay Input Boundary](F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md) | Accepted / F23D Applied |
| F24 | [Unity Build Surface Lifecycle Wiring](F24-ADR-UNITY-BUILD-001-Unity-Build-Surface-Lifecycle-Wiring.md) | Accepted / Planned after F23 |
| F25 | [Gameplay Adapter Foundation](F25-ADR-GAMEPLAY-001-Gameplay-Adapter-Foundation.md) | Deferred / After F24 |
| F25+ | [Advanced Consumers Boundary](F17-ADR-CONSUMERS-001-Advanced-Consumers-Boundary.md) | Deferred / After Gameplay Adapter Foundation |

## Rule

Past ADRs record accepted/applied decisions. Future ADRs guide implementation and should be improved before each phase starts. An in-progress ADR must distinguish implemented evidence from proposed remaining cuts.

F12 decision note: Cycle Reset Unity Event Bridges are optional. The trigger is the primary component; bridges only expose result callbacks in the Inspector.

F13 decision note: Object Entry is a passive lifecycle-owned lógical catalog/snapshot with typed owners, scoped collection and deterministic snapshot invalidation/refresh. It is not a GameObject binding, mutable registry, reset inventory or service locator.

F14 decision note: Object Reset targets only a current `ObjectEntryId + owner + scope`, uses one canonical `IObjectResetParticipant` contract and an explicit participant source, exposes Runtime Host/trigger/optional bridge UX, and does not execute Unity adapters or gameplay reset.


F15 closure note: Unity Reset Adapters are technical Unity consumers of Object Reset. F15 closed explicit participant source registration, Transform local baseline reset, required adapter/baseline guardrails, authoring UX and closure smoke. Player/Actor/Pooling/Gameplay reset remains outside F15.


F16 closure note: GameObject Active State Reset is a primitive Unity adapter for `activeSelf` only. It is not Player, Actor, NPC, Timer, Pooling or gameplay reset. Contextual reset consumers remain future work.

F17 closure note: Gate comes before Transition and Pause. Gate is not UI, readiness or input system; it decides admission of request, input, interaction or gameplay in explicit scopes and must produce decision/result/facts. F17B added passive primitives. F17C routes existing in-flight Route/Activity/CycleReset/ObjectReset request admission through Gate decisions without adding a global registry or changing the happy path. F17D added a synthetic QA smoke that validates allowed and blocked `GateEvaluationResult` diagnostics for request-admission scenarios. F17E closes Gate Foundation, documents usage in `Documentation~/Guides/F17-Gate-Foundation-Usage.md`, and hands off to F18 Transition Orchestration.

F18 closure note: Transition is flow orchestration that consumes Gate. F18A accepts the implementation plan. F18B adds passive primitives under `Runtime/Transition`: operation identity, kind, phase/status, step, plan, result and snapshot. F18C adds `Run Transition Diagnostics Smoke`, validating valid/warning/failed plan/result/snapshot shapes without scene changes. F18D adds `TransitionGateBlockerPolicy` and `Run Transition Gate Blocker Smoke`, validating that a running Transition can describe a Gate blocker and that completed/failed operations release the synthetic blocker relationship. F18E adds `TransitionOrchestrationObservationPolicy` and `Run Transition Orchestration Observation Smoke`, validating passive Route/Activity observation without executing requests. F18F closes Transition Orchestration, documents usage in `Documentation~/Guides/F18-Transition-Orchestration-Usage.md`, and hands off to F19. Fade/loading/curtain are F19 adapters/effects after the logical contract, not a substitute for Gate.


F19 closure note: Transition effects are adapters/consumers of F18 Transition Orchestration. F19A accepts the boundary and implementation plan only. F19B adds passive primitives under `Runtime/TransitionEffects`: effect identity, kind, requiredness, status, request, result, plan and snapshot. F19C adds `Run Transition Effect Diagnostics Smoke` for passive request/plan/result/snapshot diagnostics. F19D adds the first concrete Unity adapter boundary: `ITransitionEffectAdapter`, `UnityFadeCurtainEffectAdapter` and `Run Unity Fade Curtain Effect Adapter Smoke`. The adapter mutates only CanvasGroup/surface active state and does not use DOTween, registry, lifecycle ownership, Pause, Input or gameplay. The canonical smoke uses a transient QA object; optional manual visual setup is documented in `Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md`. F19E adds `TransitionEffectAuthoringPolicy`, policy issue/evaluation primitives and `Run Transition Effect Policy Guardrails Smoke`; required adapter absence and duplicate effect ids block, optional adapter absence warns without blocking, and no ScriptableObject/registry is introduced. F19F closes the phase with `Documentation~/Guides/F19-Transition-Effects-Usage.md` and compacts QA Canvas by keeping baseline smokes visible and collapsing phase diagnostics behind toggles.

F20/F23 pause note: Pause is state plus Gate blocker. F20A accepted the implementation plan; F20B added passive Pause primitives under `Runtime/Pause`; F20C added `Run Pause Diagnostics Smoke`; F20D added `Run Pause Gate Blocker Smoke`; F20E added `Run Pause Runtime Request Smoke` and the minimal in-memory request path through `FrameworkRuntimeHost`; F20F closed the phase with `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`. F20 remains asset-free: no scene object, Canvas, prefab or ScriptableObject is required. Pause is not Activity, does not own Route/Activity lifecycle and does not define `Time.timeScale` as the canonical contract. Pause overlay/content/input move to F23, after Save and Loading boundaries.

F21 Save note: F21A opened Save before Pause visual/gameplay work. F21B added passive Snapshot envelope primitives under `Runtime/Snapshot`. F21C added backend-agnostic Snapshot participant contracts and `Run Snapshot Participant Diagnostics Smoke`. F21D added `Runtime/Preferences`, `IPreferencesStore`, `PlayerPrefsPreferencesStore` and `Run Preferences Store Diagnostics Smoke`. F21E added `Runtime/ProgressionSave`, logical slot/record/backend identities, payload/record/manifest primitives, status/result primitives and `IProgressionSaveStore`. F21F added `JsonProgressionSaveStore` and `Run Progression Save JSON Backend Smoke`. F21G added `ProgressionSaveRuntime`, explicit save/load/delete requests, passive save moments and `Run Progression Save Runtime Request Smoke`. F21H closes the phase with `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. Snapshot owns envelope/participant shape and does not know backend. Preferences owns user/application settings and does not use progression slots. PlayerPrefs is only a Preferences adapter and uses explicit type markers to avoid silent fallback. Progression Save owns slots/manifests/request contracts and uses a replaceable backend port. The JSON backend is the initial adapter only, not the canonical contract; a future premium/backend-specific implementation must replace it behind the same interface.

F22 Loading closure note: F22 closes Loading as operation/progress/readiness reporting. It is not fade, curtain, loading screen prefab, TransitionEffect vocabulary or a SceneLifecycle replacement. F22A reconciles existing loading-like concepts; F22B adds passive primitives; F22C adds pure weighted progress aggregation and `Run Loading Progress Aggregation Smoke`; F22D adds `LoadingObservationAdapter` and `Run Loading Observation Adapter Smoke`; F22E adds the visual-facing `ILoadingScreenAdapter` boundary and smoke without UI implementation; F22F creates `Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`; F22G adds readiness observation primitives; F22H adds result/issue primitives. F22 is closed before F23.

F24 Unity Build note: F24 is the framework-owned Unity Build Surface / Lifecycle Wiring phase between F23 Pause Content/Overlay/Input and F25 Gameplay Adapter Foundation. F24 proves framework contracts on real Unity surfaces with minimal objects, prefabs or explicit scene wiring when applicable. It does not create gameplay adapters, Player/Actor/NPC/Door/Inventory/Combat adapters, full menus, a full UI system, a full save system or product gameplay.

F25 defer note: Advanced Consumers, Gameplay Adapter Foundation and contextual reset for Player/Actor/NPC/Timer/Door/Pickup are deferred until F24 proves the framework Unity build surfaces and lifecycle wiring.

F21A applied note: F21A is documentation-only. It realigns roadmap/ADRs, accepts the Save/Snapshot/Preferences/Progression boundary, accepts the Loading Operation/Progress/Readiness boundary for F22 and moves Pause Content/Overlay/Input to F23. F24 later becomes Unity Build Surface / Lifecycle Wiring, and Gameplay Adapter Foundation moves to F25. No runtime, asmdef, backend, PlayerPrefs, JSON, UI, scene object, prefab or ScriptableObject is added by F21A. Next cut after F21A: F21B - Snapshot Envelope Primitives.


F21B applied note: F21B adds passive Snapshot Envelope primitives only: `SnapshotEnvelopeId`, `SnapshotScope`, `SnapshotSchemaId`, `SnapshotSchemaVersion`, `SnapshotPayloadFormat`, `SnapshotPayload` and `SnapshotEnvelope`. It adds `Snapshot` to `FrameworkIdentityDomain`. It does not add backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave moments, participant contracts, capture/restore runtime, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21C applied note: F21C adds backend-agnostic Snapshot participant contracts: `SnapshotParticipantId`, `SnapshotParticipantRequiredness`, `SnapshotParticipantResultStatus`, `SnapshotParticipantDescriptor`, `SnapshotCaptureContext`, `SnapshotRestoreContext`, `SnapshotParticipantCaptureResult`, `SnapshotParticipantRestoreResult` and `ISnapshotParticipant`. It adds `Run Snapshot Participant Diagnostics Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. The smoke is synthetic and validates the contract without backend, PlayerPrefs, JSON, slots, UI, scene objects or orchestration runtime. Known runtime diagnostic snapshots such as `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot` and `ObjectEntryRuntimeContextSnapshot` remain outside the canonical Save Snapshot namespace.

F21D applied note: F21D adds Preferences store contracts and PlayerPrefs adapter: `PreferenceKey`, `PreferenceValueKind`, `PreferenceValue`, `PreferenceReadStatus`, `PreferenceWriteStatus`, `PreferenceReadResult`, `PreferenceWriteResult`, `IPreferencesStore` and `PlayerPrefsPreferencesStore`. It adds `Preferences` to `FrameworkIdentityDomain` and `Run Preferences Store Diagnostics Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. PlayerPrefs is limited to Preferences, writes a type marker beside each key and does not define Snapshot backend, Progression Save slot/manifest, JSON, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21E applied note: F21E adds Progression Save port and slot/manifest primitives: `ProgressionSaveSlotId`, `ProgressionSaveRecordId`, `ProgressionSaveBackendId`, `ProgressionSavePayloadFormat`, `ProgressionSavePayload`, `ProgressionSaveSlotRecord`, `ProgressionSaveManifestEntry`, `ProgressionSaveManifest`, `ProgressionSaveReadStatus`, `ProgressionSaveWriteStatus`, `ProgressionSaveDeleteStatus`, `ProgressionSaveReadResult`, `ProgressionSaveWriteResult`, `ProgressionSaveDeleteResult`, `ProgressionSaveManifestReadResult`, `ProgressionSaveManifestWriteResult` and `IProgressionSaveStore`. It adds `ProgressionSave` to `FrameworkIdentityDomain`. F21E does not add JSON, file paths, PlayerPrefs, concrete backend, autosave/load moments, runtime request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21F applied note: F21F adds `JsonProgressionSaveStore` behind `IProgressionSaveStore` and `Run Progression Save JSON Backend Smoke`. JSON/file paths are adapter details, not canonical identities. F21F does not add Snapshot backend usage, Preferences usage, PlayerPrefs, autosave/load moments, runtime request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21G applied note: F21G adds `ProgressionSaveRuntime`, `ProgressionSaveRequest`, `ProgressionSaveRequestResult`, `ProgressionSaveMoment` and related ids/enums. Runtime requests execute explicit save/load/delete operations against `IProgressionSaveStore`. Autosave moments are passive descriptors only; F21G does not add an autosave scheduler, Route/Activity hook, Snapshot capture orchestration, Preferences/PlayerPrefs usage, UI, scene object, prefab, ScriptableObject or asmdef changes. It adds `Run Progression Save Runtime Request Smoke`.

F21H closure note: F21 is closed as Save / Snapshot / Preferences / Progression Save Foundation. Use `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`.

F22A applied note: F22A is documentation-only. It accepts the Loading architecture plan, updates the roadmap/ADR index and records the audit that prevents loading ghosts/orphans across SceneLifecycle, Route Scene Composition, Transition and TransitionEffects. It adds no runtime code, asmdef changes, primitives, progress aggregator, smoke, fade, curtain, loading screen prefab, UI, scene object, prefab, ScriptableObject, SceneLifecycle replacement, Transition replacement, backend, PlayerPrefs or JSON. Current next cut: F23E - Pause Content / Overlay / Input Diagnostics Smoke.


F22B applied note: F22B adds the canonical `Immersive.Framework.Loading` primitive namespace and `FrameworkIdentityDomain.Loading`. It creates operation id, step id, operation/step statuses, normalized progress, step weight, weighted progress, step and operation records.

F22C applied note: F22C adds `LoadingProgressAggregationStatus`, `LoadingProgressAggregationResult`, `LoadingProgressAggregator` and `Run Loading Progress Aggregation Smoke` under QA Canvas `Show Loading diagnostics`. It does not add SceneLifecycle/Transition observation adapter, visual adapter, UI, fade, curtain, loading screen prefab or lifecycle execution.

F22D applied note: F22D adds `LoadingObservationAdapter` and `Run Loading Observation Adapter Smoke`. It observes SceneLifecycle and Transition diagnostics as canonical Loading progress data without executing lifecycle, replacing Transition, running effects or creating UI. Current next cut: F23E - Pause Content / Overlay / Input Diagnostics Smoke.

F22E applied note: F22E adds `ILoadingScreenAdapter`, `LoadingScreenPresentation`, `LoadingScreenAdapterAction`, `LoadingScreenAdapterStatus`, `LoadingScreenAdapterResult` and `Run Loading Screen Adapter Boundary Smoke`. The loading screen boundary is visual-facing only and consumes canonical `LoadingOperation`; it does not create UI, scene objects, prefabs, ScriptableObjects, fade, curtain, TransitionEffects execution, SceneLifecycle execution, Transition execution or readiness mutation. Current next cut: F23E - Pause Content / Overlay / Input Diagnostics Smoke.


F22F closure note: Loading Operation / Progress / Readiness Boundary is closed. The guide documents primitive construction, weighted aggregation, observation mapping, loading screen adapter implementation expectations and QA smokes. No UI prefab, scene object, ScriptableObject, fade/curtain execution, SceneLifecycle replacement, Transition replacement, readiness mutation, backend, PlayerPrefs, JSON or asmdef change was added by the closure cut.


## F22G pre-F23 debt closure

- `F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md` — updated with `F22G — Loading Readiness Observation Primitives`.


## F22H pre-F23 debt closure

- `F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md` — updated with `F22H — Loading Result / Issue Primitive Closure`.


## F23A Pause Content / Overlay / Input ADR Plan

- `F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md` — accepted as the F23 boundary plan.
- F23 consumes F20 Pause core, F21 Save/Preferences and F22 Loading reporting without redefining them.
- F23A adds no runtime code, asmdef changes, UI, prefab, scene object, ScriptableObject, input asset binding, `Time.timeScale` policy or gameplay adapter.
- F23B adds passive Pause Content Anchor consumer contracts under `Runtime/Pause`: request identity, purpose, request/result/status records and `IPauseContentAnchorConsumer`.
- F23B prepares canonical `ContentAnchorBindingRequest` data for future adapters without creating anchors, UI, input bindings, lifecycle ownership, `Time.timeScale` policy or gameplay adapters.
- F24 is the next framework phase after F23 closes: Unity Build Surface / Lifecycle Wiring.
- F25 is Gameplay Adapter Foundation, after F24.
- Immediate next cut is F23E - Pause Content / Overlay / Input Diagnostics Smoke.

## F23 Pause Content / Overlay / Input

- F23A accepted the Pause Content / Overlay / Input boundary.
- F23B added Pause Content Anchor consumer contracts.
- F23C added Pause Overlay adapter boundary contracts.
- F23D added device-agnostic Pause Input boundary contracts: `PauseInputActionId`, `PauseInputCommandKind`, `PauseInputSourceKind`, `PauseInputSignal`, `PauseInputResolutionStatus`, `PauseInputResolutionResult` and `IPauseInputResolver`.
- F23D does not create Input System assets, action maps, device bindings, UI navigation, Time.timeScale policy, lifecycle ownership or gameplay adapters.

## F24 Unity Build Surface / Lifecycle Wiring

- `F24-ADR-UNITY-BUILD-001-Unity-Build-Surface-Lifecycle-Wiring.md` — accepted as the F24 boundary plan.
- F24 comes after F23 and before F25 Gameplay Adapter Foundation.
- F24 is framework-owned Unity build/lifecycle wiring, not gameplay.
- F24B — Transition ↔ GameFlow Runtime Integration — is the first technical cut of F24 because RouteRequestTrigger / GameFlow must pass through a real TransitionPlan before curtain/loading visuals can prove the lifecycle path.
- F24 follows the F19D pattern: minimal object, explicit wiring, real smoke, limited scope.
- F24 adds no runtime, asmdef, prefab, scene object, ScriptableObject, UI or asset in this documentation cut.
