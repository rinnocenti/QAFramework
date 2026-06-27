# Immersive Framework

Unity package for the Immersive Framework lifecycle architecture.

The canonical framework plan is:

```text
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions are indexed at:

```text
Documentation~/ADRs/ADR-INDEX.md
```


Usage guides are kept under:

```text
Documentation~/Guides/
```

Closed phases must add or update a `Usage` guide there. Current closed usage guides include:

```text
Documentation~/Guides/F17-Gate-Foundation-Usage.md
Documentation~/Guides/F18-Transition-Orchestration-Usage.md
Documentation~/Guides/F19-Transition-Effects-Usage.md
Documentation~/Guides/F20-Pause-State-Gate-Usage.md
Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md
Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md
```


Status:

```text
F0-F22 closed/applied. F21 is closed with F21H usage documentation. F22 is closed through F22H after pre-F23 framework-only debt closure. F22A Loading Architecture ADR Plan is applied. F22B Loading Operation / Step / Weighted Progress Primitives is applied. F22C Loading Progress Aggregation Smoke is applied. F22D SceneLifecycle / Transition Loading Observation Adapter is applied. F22E Loading Screen Adapter Boundary is applied. F22F Closure + Usage Guide is applied. F22G Loading Readiness Observation Primitives is applied. F22H Loading Result / Issue Primitive Closure is applied. F23A Pause Content / Overlay / Input ADR Plan is applied. F23E Pause Boundary Intent Smoke + Adapter Deferral is applied.
F17 is Gate Foundation and is closed through F17E. F18 is Transition Orchestration Foundation and is closed through F18F. F19 is Transition Effects and is closed through F19F. F20 is Pause State/Gate and is closed through F20F with `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`. F20A accepted the Pause State/Gate implementation plan. F20B introduced passive Pause state primitives under `Runtime/Pause`. F20C added `Run Pause Diagnostics Smoke`. F20D added passive Pause-to-Gate blocker policy. F20E added the minimal runtime Pause request path through `FrameworkRuntimeHost` and `PauseRuntime`. F20F closes the phase and hands off to F21 Save / Snapshot / Preferences / Progression Save Foundation. F21A realigns the roadmap and accepts Save/Loading boundaries; F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`; F21C adds Snapshot participant contracts and a synthetic diagnostics smoke; F21D adds Preferences store contracts and the PlayerPrefs adapter; F21E adds Progression Save port, slot, record and manifest primitives; F21F adds the JSON backend adapter and diagnostics smoke; F21G adds the explicit Progression Save runtime request path and passive autosave/manual moment contracts; F21H closes the phase with `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md` and hands off to F22 Loading. F22A accepts the Loading architecture plan and reconciles existing loading-like concepts so SceneLifecycle, Transition and TransitionEffects do not become parallel Loading tracks. F22B adds passive Loading primitives; F22C adds weighted aggregation; F22D adds observation adapters for SceneLifecycle/Transition diagnostics. F22E adds loading screen adapter contracts and a synthetic boundary smoke without UI/prefab implementation.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C routes existing in-flight request admission through Gate; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase without adding Pause, Transition runtime, UI or gameplay. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives. F18C adds a synthetic Transition diagnostics smoke for plan/result/snapshot shapes. F18D adds a passive Transition-to-Gate blocker relationship and smoke. F18E adds a passive Route/Activity orchestration observation policy and smoke. F18F closes the phase with a Transition Orchestration usage guide and hands off to F19. F19A accepts the Transition Effects boundary/implementation plan. F19B introduces passive Transition Effect primitives under `Runtime/TransitionEffects`. F19C adds `Run Transition Effect Diagnostics Smoke`. F19D adds the minimal built-in Unity `UnityFadeCurtainEffectAdapter`, adapter contract and `Run Unity Fade Curtain Effect Adapter Smoke`. F19E adds required/optional effect policy guardrails and `Run Transition Effect Policy Guardrails Smoke`, using explicit adapter lists only. F19F closes the phase with `Documentation~/Guides/F19-Transition-Effects-Usage.md`, preserves the asset-free policy boundary, and compacts QA Canvas by hiding phase diagnostics behind foldouts. F20A accepted the Pause State/Gate implementation plan: Pause is state plus Gate blocker relationship, not Activity, menu, overlay, input system or `Time.timeScale` contract. F20B adds passive Pause primitives: `PauseRequestId`, `PauseState`, `PauseRequestKind`, `PauseRequestStatus`, `PauseIssue`, `PauseRequest`, `PauseResult` and `PauseSnapshot`. F20C validates pause/resume/toggle/idempotent/rejected/snapshot shapes through a synthetic QA smoke under the collapsed Pause diagnostics group. F20D validates Pause-to-Gate blockers. F20E adds the minimal request path. F20F closes the phase with the F20 usage guide. Fade/loading/curtain are F19 adapters/effects and are not core Transition.
```

F15-F16 reset adapter closure:

```text
Unity Reset Adapters mínimos are closed with:
- explicit Unity participant source;
- Transform Reset Participant with authored local baseline;
- required adapter/baseline guardrails;
- authoring UX and guide;
- closure smoke.

F16 then added GameObject activeSelf reset as a primitive technical adapter.
```

Current reset boundary:

```text
Cycle Reset covers Route/Activity cycle reset.
Object Reset foundation provides logical orchestration.
F15 added Transform local baseline reset. F16 added GameObject activeSelf baseline reset.
Rigidbody, Animator, Player/Actor, Pooling and Gameplay reset remain future work.
Contextual reset for Player/Actor/NPC/Timer/Door/Pickup is deferred until after Gate, Transition and Pause, and after a mature gameplay object model exists.
```

Current planning axis:

```text
F17 - Gate Foundation / CLOSED
F18 - Transition Orchestration Foundation / CLOSED
F19 - Transition Effects / CLOSED
F20 - Pause State and Pause Gate / CLOSED / F20F QA PASS + USAGE
F21 - Save / Snapshot / Preferences / Progression Save Foundation / CLOSED / F21H QA PASS + USAGE
F22 - Loading Operation / Progress / Readiness Boundary / CLOSED / F22H QA PASS
F23 - Pause Content / Overlay / Input Boundary / F23E APPLIED / NEXT F23F
F24 - Unity Build Surface / Lifecycle Wiring
F25 - Gameplay Adapter Foundation
```


### F23A — Pause Content / Overlay / Input ADR Plan

F23A accepts the Pause Content / Overlay / Input boundary as documentation-only. F23 consumes F20 Pause core, F21 Save/Preferences boundaries and F22 Loading reporting without redefining them.

F23A does not add runtime code, asmdef changes, UI, Canvas, prefab, ScriptableObject, Input System asset binding, `Time.timeScale` adapter, lifecycle owner or gameplay adapter.


### F23E — Pause Boundary Intent Smoke + Adapter Deferral

F23E corrects F23 before materialization. F23B/F23C/F23D are reclassified from adapter/consumer language to framework-only intent/requirement language.

F23 now keeps only intent records under `Runtime/Pause`:

```text
PauseContentRequirementId
PauseContentRequirementPurpose
PauseContentRequirement
PausePresentationIntent
PauseInputActionId
PauseInputCommandKind
PauseInputSourceKind
PauseInputSignal
PauseInputIntent
```

Removed/deferred from F23:

```text
IPauseContentAnchorConsumer
PauseContentAnchorConsumerResult
IPauseOverlayAdapter
PauseOverlayAdapterResult
IPauseInputResolver
PauseInputResolutionResult
ContentAnchorBindingRequest preparation
overlay show/update/hide execution
input dispatch to PauseRequest
```

F23E adds `Run Pause Boundary Intent Smoke`. The smoke proves content requirement, presentation intent and input intent without anchor binding, overlay adapter, input adapter, Input System, UI, `Time.timeScale`, Unity Build materialization or gameplay adapters.

Next cut: `IF-FW-F23F — Closure + Usage Guide`.

After F23 closes, the official order is:

```text
F24 — Unity Build Surface / Lifecycle Wiring
F25 — Gameplay Adapter Foundation
```

F24 is the framework-owned phase that proves contracts in real Unity surfaces before gameplay adapters. It follows the F19D pattern: minimal object, explicit wiring, real smoke and limited scope. F24B — Transition ↔ GameFlow Runtime Integration — must be the first technical cut of F24 so curtain/loading visual work cannot bypass the real lifecycle path.


F19 transition effects note:

```text
F19A is documentation/plan only. F19B adds passive effect primitives only. F19C adds synthetic diagnostics smoke only.
F19D adds a minimal Unity fade/curtain adapter boundary. The canonical smoke creates a transient QA object, so no project scene asset is required to validate compile/smoke. For manual visual testing, create a scene GameObject with CanvasGroup and UnityFadeCurtainEffectAdapter; see `Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md`.
F19E adds policy/authoring guardrails for required/optional adapters. It does not create a ScriptableObject or registry; the policy evaluates an explicit adapter list supplied by the caller/smoke.
F19F closes the phase with `Documentation~/Guides/F19-Transition-Effects-Usage.md` and compacts QA Canvas: only baseline smokes stay visible by default; Gate/Transition/Effect, Route/Content, Foundation and Reset/Object diagnostics are collapsed.
```

F20 pause note:

```text
F20B adds passive Pause primitives under `Runtime/Pause`. No scene object, Canvas, prefab or ScriptableObject is required.
F20 remains the logical Pause core: state, request/result, snapshot/facts, policy and Gate blocker relationship. F20C remains synthetic and does not mutate runtime state.
Save/Snapshot/Preferences/Progression Save are F21 boundaries. Loading operation/progress/readiness is F22. Pause intent/requirement contracts move to F23. Concrete Pause overlay/input binding moves to F24.
F20C adds `Run Pause Diagnostics Smoke` under `Show Pause diagnostics`, validating passive Pause requests/results/snapshots. F20D adds `Run Pause Gate Blocker Smoke`. F20E adds `Run Pause Runtime Request Smoke`, which exercises the real in-memory Pause request path and leaves the runtime in `Running`. F20F closes the phase with `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`.
```


### F20D — Pause-to-Gate Blocker Policy

F20D adds passive Pause-to-Gate blocker policy diagnostics. It does not create a Pause runtime owner, input binding, overlay, Time.timeScale adapter or Gate registry.

### F20E — Minimal Runtime Pause Request Path

F20E adds `PauseRuntime` and `FrameworkRuntimeHost.RequestPause(...)`. It mutates only in-memory Pause state and derived Pause Gate snapshots. It does not read input, show overlay/menu UI, change `Time.timeScale`, own Route/Activity lifecycle or register blockers in a global Gate runtime.


### F20F — Pause State/Gate Closure

F20 is closed as logical Pause core. It provides state, request/result, runtime snapshot and passive Gate blocker relationship. It intentionally does not provide Pause menu, overlay content, input binding, `Time.timeScale` adapter, scene object setup or ScriptableObject authoring.

Use `Documentation~/Guides/F20-Pause-State-Gate-Usage.md` for the F20 Pause core usage boundary and QA smokes. F21 is closed with `Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. F21A realigned Save/Loading; F21B added Snapshot Envelope primitives; F21C added Snapshot participant contracts and `Run Snapshot Participant Diagnostics Smoke`; F21D added Preferences store contracts, `PlayerPrefsPreferencesStore` and `Run Preferences Store Diagnostics Smoke`; F21E added Progression Save port, slot, record and manifest primitives; F21F added `JsonProgressionSaveStore` and `Run Progression Save JSON Backend Smoke`; F21G added `ProgressionSaveRuntime`, explicit save/load/delete requests, passive autosave/manual moment contracts and `Run Progression Save Runtime Request Smoke`; F21H closes the phase and hands off to F22 Loading.

### F21/F22 roadmap realignment

F21A opened Save before Pause visual/gameplay work. Snapshot is an envelope/participant boundary and does not know persistence backend. Preferences are independent user/application settings and do not use progression slots. Progression Save owns slot/manifest/request contracts and uses a replaceable backend port. The JSON backend added in F21F is only the initial adapter, not the canonical contract; a future premium backend must swap behind the same interface.

F22 is now reserved for Loading as operation/progress/readiness reporting. Loading is not fade, curtain, loading screen prefab, TransitionEffect vocabulary or a SceneLifecycle replacement. F22A records the audit that avoids ghost/parallel loading tracks: SceneLifecycle still executes scene operations, Route Scene Composition still owns route scene evidence, Transition still owns flow orchestration, and TransitionEffects `LoadingScreen` / `LoadingProgress` remain visual/effect-facing effect names. Loading visuals remain a later adapter boundary.




### F22A — Loading Architecture ADR Plan Result

F22A is documentation-only. It accepts the Loading Operation / Progress / Readiness architecture plan and updates the roadmap/ADR index. It does not add runtime code, asmdefs, primitives, progress aggregator, smoke, fade, curtain, loading screen prefab, UI, scene object, prefab, ScriptableObject, SceneLifecycle replacement, Transition replacement, backend, PlayerPrefs or JSON.

Canonical F22 Loading remains separate from existing loading-like names: `SceneLifecycle` executes Unity scene load/unload, Route Scene Composition reports route scene composition evidence, Transition owns flow orchestration, and TransitionEffects `LoadingScreen` / `LoadingProgress` remain visual/effect-facing vocabulary. F22B starts the single canonical Loading primitive namespace under `Runtime/Loading`.

Current next cut: `F23F — Closure + Usage Guide`.

### F22B — Loading Operation / Step / Weighted Progress Primitives

F22B adds passive Loading primitives under `Runtime/Loading` and the `Immersive.Framework.Loading` namespace:

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

These primitives are operation/progress/readiness-facing data. They do not execute scene loads, replace Transition, show UI, create a loading screen prefab, run fade/curtain effects, persist saves, use PlayerPrefs or create lifecycle hooks.

F22B also adds `FrameworkIdentityDomain.Loading` so Loading operation/step ids do not borrow identity domains from SceneLifecycle, Transition, TransitionEffects, Save, Pause or UI.

Current next cut: `F23F — Closure + Usage Guide`.


### F22C — Loading Progress Aggregation Smoke

F22C adds pure weighted progress aggregation under `Runtime/Loading`:

```text
LoadingProgressAggregationStatus
LoadingProgressAggregationResult
LoadingProgressAggregator
```

It also adds `Run Loading Progress Aggregation Smoke` under QA Canvas `Show Loading diagnostics`. The smoke validates weighted running progress, completed-with-skipped aggregation, failed aggregation, no-step aggregation and the canonical boundary.

F22C does not add SceneLifecycle/Transition observation adapters, readiness wait contracts, LoadingResult/LoadingFailure records, UI, fade, curtain, loading screen prefab, scene object, prefab, ScriptableObject, backend, PlayerPrefs, JSON or asmdef changes.

### F22D — SceneLifecycle / Transition Loading Observation Adapter

F22D adds `LoadingObservationAdapter` under `Runtime/Loading` and `Run Loading Observation Adapter Smoke` under QA Canvas `Show Loading diagnostics`. The adapter maps existing `SceneLifecycleLoadResult`, `SceneLifecycleUnloadResult` and `TransitionResult`/`TransitionStep` diagnostics into canonical `LoadingStep`, `LoadingProgressAggregationResult` and `LoadingOperation` records.

F22D is observation-only. It does not execute SceneLifecycle, replace Transition, run TransitionEffects, mutate readiness, create UI, show fade/curtain, instantiate a loading screen prefab, add scene objects, add ScriptableObjects, use backend/PlayerPrefs/JSON or change asmdefs.

Current next cut: `F23F — Closure + Usage Guide`.
### F21A — Save/Loading ADR Plan Result

F21A is documentation-only. It does not add runtime code, asmdefs, PlayerPrefs, JSON, backend, UI, scene object, prefab or ScriptableObject setup. It records that Snapshot, Preferences and Progression Save are separate boundaries, and that Loading Operation/Progress/Readiness is its own F22 boundary before Pause visual work.

F21B adds passive Snapshot primitives under `Runtime/Snapshot`: `SnapshotEnvelopeId`, `SnapshotScope`, `SnapshotSchemaId`, `SnapshotSchemaVersion`, `SnapshotPayloadFormat`, `SnapshotPayload` and `SnapshotEnvelope`. These types do not save files, use PlayerPrefs, choose JSON, bind progression slots, restore gameplay, create runtime objects or define participant contracts.

F21C adds backend-agnostic Snapshot participant contracts and a synthetic diagnostics smoke. It does not add discovery, orchestration runtime, backend, PlayerPrefs, JSON, progression slots or UI.

F22A result: documentation-only architecture plan accepted. Current next cut: `F23F — Closure + Usage Guide`.


### F21B — Snapshot Envelope Primitives

F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`. The envelope can identify a captured payload by envelope id, scope, owner identity, schema id, schema version, payload format, captured UTC ticks, source and reason.

F21B intentionally does not add a backend, PlayerPrefs, JSON, progression slot, manifest, autosave moment, participant interface, capture/restore runtime, UI, scene object, prefab, ScriptableObject or asmdef change.

### F21C — Snapshot Participant Contracts + Diagnostics Smoke

F21C adds the first backend-agnostic participant surface under `Runtime/Snapshot`:

```text
SnapshotParticipantId
SnapshotParticipantRequiredness
SnapshotParticipantResultStatus
SnapshotParticipantDescriptor
SnapshotCaptureContext
SnapshotRestoreContext
SnapshotParticipantCaptureResult
SnapshotParticipantRestoreResult
ISnapshotParticipant
```

F21C also adds `Run Snapshot Participant Diagnostics Smoke` under `Show Save / Snapshot diagnostics` in the QA Canvas. The smoke validates descriptor/context matching, synthetic capture, synthetic restore, foreign-envelope rejection, optional skip semantics and known non-save diagnostic snapshot boundaries.

F21C deliberately does not add participant discovery, Snapshot orchestration runtime, backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave/load moments, UI, scene object, prefab, ScriptableObject or asmdef change. Existing `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot` and `ObjectEntryRuntimeContextSnapshot` remain diagnostic/runtime state snapshots outside the canonical Save Snapshot namespace.


### F21D — Preferences Store Contracts + PlayerPrefs Backend

F21D adds the canonical Preferences boundary under `Runtime/Preferences`:

```text
PreferenceKey
PreferenceValueKind
PreferenceValue
PreferenceReadStatus
PreferenceWriteStatus
PreferenceReadResult
PreferenceWriteResult
IPreferencesStore
PlayerPrefsPreferencesStore
```

Preferences are user/application settings only. They do not use Snapshot envelopes, Progression Save slots, manifests, autosave moments or gameplay progression payloads. `PlayerPrefsPreferencesStore` is only an adapter behind `IPreferencesStore`; it is not the canonical persistence model for Progression Save.

F21D also adds `Run Preferences Store Diagnostics Smoke` under `Show Save / Snapshot diagnostics`. The smoke writes namespaced QA PlayerPrefs keys, validates typed read/write/missing/type-mismatch behavior, validates the PlayerPrefs type-marker guard, then deletes the QA keys.

F21D does not add Snapshot backend usage, Progression Save slots/manifests, JSON, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

Current next cut: `F23F — Closure + Usage Guide`.

### F21E — Progression Save Port + Slot/Manifest Primitives

F21E adds the canonical Progression Save boundary under `Runtime/ProgressionSave`:

```text
ProgressionSaveSlotId
ProgressionSaveRecordId
ProgressionSaveBackendId
ProgressionSavePayloadFormat
ProgressionSavePayload
ProgressionSaveSlotRecord
ProgressionSaveManifestEntry
ProgressionSaveManifest
ProgressionSaveReadStatus
ProgressionSaveWriteStatus
ProgressionSaveDeleteStatus
ProgressionSaveReadResult
ProgressionSaveWriteResult
ProgressionSaveDeleteResult
ProgressionSaveManifestReadResult
ProgressionSaveManifestWriteResult
IProgressionSaveStore
```

Progression Save now has logical slot identity, stored record identity, manifest metadata, backend-agnostic payload bytes and a replaceable store port. The port is the framework contract; JSON, cloud, encrypted or premium plugin storage must live behind it.

F21E deliberately does not add a concrete backend, JSON serialization, file paths, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef change. Snapshot remains the capture/restore envelope boundary. Preferences remains user/application settings only.

Current next cut: `F23F — Closure + Usage Guide`.

### F21F — JSON Progression Backend + Diagnostics Smoke

F21F adds the first concrete Progression Save backend adapter:

```text
JsonProgressionSaveStore
Run Progression Save JSON Backend Smoke
```

`JsonProgressionSaveStore` implements `IProgressionSaveStore`. JSON files, physical paths, manifest file names and slot file names are adapter details. Framework consumers must keep depending on the port, so a future cloud, encrypted or premium backend can replace the adapter without changing the framework-facing contract.

The diagnostics smoke validates backend identity, missing manifest/slot results, write/read roundtrip, manifest update, corrupt slot detection, delete cleanup and canonical boundary separation. It writes only under Unity temporary cache QA storage and removes that storage at the end.

F21F deliberately does not add Snapshot backend usage, Preferences usage, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef change.

### F21G — Progression Save Runtime Request Path + Autosave Moment Contracts

F21G adds the explicit Progression Save runtime request path under `Runtime/ProgressionSave`:

```text
ProgressionSaveRequestId
ProgressionSaveMomentId
ProgressionSaveRequestKind
ProgressionSaveMomentKind
ProgressionSaveRequestStatus
ProgressionSaveMoment
ProgressionSaveRequest
ProgressionSaveRequestResult
ProgressionSaveRuntime
```

`ProgressionSaveRuntime` executes explicit save/load/delete requests against an injected `IProgressionSaveStore`. `ProgressionSaveMoment` describes manual, autosave, checkpoint or lifecycle-boundary intent, but does not schedule autosave or observe Route/Activity lifecycle by itself.

QA adds:

```text
Run Progression Save Runtime Request Smoke
```

F21G deliberately does not add Snapshot capture orchestration, Preferences usage, PlayerPrefs usage, autosave scheduler, Route/Activity lifecycle hook, UI, scene object, prefab, ScriptableObject or asmdef change.

Current next cut: `F23F — Closure + Usage Guide`.


### F22E — Loading Screen Adapter Boundary

F22E adds the loading screen adapter boundary under `Runtime/Loading`:

```text
ILoadingScreenAdapter
LoadingScreenPresentation
LoadingScreenAdapterAction
LoadingScreenAdapterStatus
LoadingScreenAdapterResult
```

It also adds `Run Loading Screen Adapter Boundary Smoke` under QA Canvas `Show Loading diagnostics`.

This is not a loading screen prefab and not a UI implementation. It is the port future loading screen UI will implement so it consumes canonical `LoadingOperation` progress instead of creating a parallel model under UI, TransitionEffects, SceneLifecycle or Pause.

F22E does not create UI, scene objects, prefabs, ScriptableObjects, fade, curtain, TransitionEffect execution, SceneLifecycle execution, Transition execution, readiness mutation, backend, PlayerPrefs, JSON or asmdef changes.

Current next cut: `F23F — Closure + Usage Guide`.


### F22F — Loading Boundary Closure

F22 is closed as Loading Operation / Progress / Readiness Boundary. Use:

```text
Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md
```

The closed boundary includes passive operation/step primitives, weighted progress aggregation, observation adapters for existing SceneLifecycle/Transition diagnostics, and a loading screen adapter port for future UI.

F22F does not add UI, scene objects, prefabs, ScriptableObjects, fade/curtain execution, SceneLifecycle replacement, Transition replacement, readiness mutation, backend, PlayerPrefs, JSON or asmdef changes.

Current next cut: `F23F — Closure + Usage Guide`.


### F22G — Loading Readiness Observation Primitives

F22G resolves the framework-only readiness debt found before F23. It adds passive readiness observation primitives under `Runtime/Loading`:

```text
LoadingReadinessObservationId
LoadingReadinessStatus
LoadingReadinessObservation
```

It also adds `Run Loading Readiness Observation Smoke` under QA Canvas `Show Loading diagnostics`. The smoke validates waiting, ready, blocked and failed readiness observations. F22G does not execute SceneLifecycle, replace Transition, run TransitionEffects, mutate Activity readiness, create UI, instantiate prefabs or introduce gameplay adapters.


### F22H — Loading Result / Issue Primitive Closure

F22H resolves the remaining framework-only Loading result debt before F23. It adds passive result and issue primitives under `Runtime/Loading`:

```text
LoadingIssueSeverity
LoadingIssue
LoadingResultStatus
LoadingResult
```

It also adds `Run Loading Result and Issue Smoke` under QA Canvas `Show Loading diagnostics`. The smoke validates success, waiting-for-readiness and failed result summaries. F22H does not execute lifecycle, retry, fallback, mutate readiness, create UI, instantiate prefabs or introduce gameplay adapters.
