# ADR Index

Canonical framework plan:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

ADRs record accepted architectural decisions. They do not replace the operational roadmap and must not redefine phase order.

F11 is closed as `Cycle Reset Foundation`. F12 is closed as `Cycle Reset Integration & Authoring UX`. F13 is closed as `Object Entry Foundation`. F14 is closed as `Local Object Reset Foundation`. F15 is closed as `Unity Reset Adapters minimos`. F16 is closed as `GameObject Active State Reset Adapter`. F17 is closed as `Gate Foundation`. F18 is closed as `Transition Orchestration Foundation`. F19 is closed as `Transition Effects`. F20 is closed as `Pause State and Gate`. F21 is open for Save / Snapshot / Preferences / Progression Save Foundation. F21A is applied as a documentation-only ADR plan; F21B Snapshot Envelope Primitives are applied; F21C Snapshot Participant Contracts + Diagnostics Smoke is applied; F21D is next. F22 is Loading Operation / Progress / Readiness Boundary. Pause Content/Overlay/Input moves to F23. Gameplay Adapter Foundation moves to F24.

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
| F21 | [Save Snapshot Preferences Progression Boundary](F21-ADR-SAVE-001-Save-Snapshot-Preferences-Progression-Boundary.md) | Accepted / F21C Participant Smoke Applied / F21D Next |
| F22 | [Loading Operation Progress Readiness Boundary](F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md) | Accepted / Created in F21A / F22A Planned |
| F23 | [Pause Content Overlay Input Boundary](F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md) | Deferred / Moved from former F21 plan |
| F24+ | [Advanced Consumers Boundary](F17-ADR-CONSUMERS-001-Advanced-Consumers-Boundary.md) | Deferred / After Gameplay Adapter Foundation |
| F24 | [Gameplay Capabilities Boundary](F18-ADR-GAMEPLAY-001-Gameplay-Capabilities-Boundary.md) | Deferred / Gameplay Adapter Foundation |

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

F21 Save note: F21A opened Save before Pause visual/gameplay work. F21B added passive Snapshot envelope primitives under `Runtime/Snapshot`. F21C added backend-agnostic Snapshot participant contracts and `Run Snapshot Participant Diagnostics Smoke`. Snapshot owns envelope/participant shape and does not know backend. Preferences owns user/application settings and does not use progression slots. Progression Save owns slots/manifests/request contracts and uses a replaceable backend port. A future JSON backend is the initial adapter only, not the canonical contract; a future premium backend must replace it behind the same interface.

F22 Loading note: F21A reserves F22 for Loading as operation/progress/readiness reporting. It is not fade, curtain, loading screen prefab or a SceneLifecycle replacement. Loading visual belongs to a later adapter boundary.

F24 defer note: Advanced Consumers, Gameplay Adapter Foundation and contextual reset for Player/Actor/NPC/Timer/Door/Pickup are deferred until Save/Loading/Pause boundaries are planned and a mature gameplay object model exists.

F21A applied note: F21A is documentation-only. It realigns roadmap/ADRs, accepts the Save/Snapshot/Preferences/Progression boundary, accepts the Loading Operation/Progress/Readiness boundary for F22, moves Pause Content/Overlay/Input to F23 and moves Gameplay Adapter Foundation to F24. No runtime, asmdef, backend, PlayerPrefs, JSON, UI, scene object, prefab or ScriptableObject is added by F21A. Next cut after F21A: F21B - Snapshot Envelope Primitives.


F21B applied note: F21B adds passive Snapshot Envelope primitives only: `SnapshotEnvelopeId`, `SnapshotScope`, `SnapshotSchemaId`, `SnapshotSchemaVersion`, `SnapshotPayloadFormat`, `SnapshotPayload` and `SnapshotEnvelope`. It adds `Snapshot` to `FrameworkIdentityDomain`. It does not add backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave moments, participant contracts, capture/restore runtime, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21C applied note: F21C adds backend-agnostic Snapshot participant contracts: `SnapshotParticipantId`, `SnapshotParticipantRequiredness`, `SnapshotParticipantResultStatus`, `SnapshotParticipantDescriptor`, `SnapshotCaptureContext`, `SnapshotRestoreContext`, `SnapshotParticipantCaptureResult`, `SnapshotParticipantRestoreResult` and `ISnapshotParticipant`. It adds `Run Snapshot Participant Diagnostics Smoke` under QA Canvas `Show Save / Snapshot diagnostics`. The smoke is synthetic and validates the contract without backend, PlayerPrefs, JSON, slots, UI, scene objects or orchestration runtime. Known runtime diagnostic snapshots such as `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot` and `ObjectEntryRuntimeContextSnapshot` remain outside the canonical Save Snapshot namespace. Next cut: F21D - Preferences Store Contracts + PlayerPrefs Backend.
