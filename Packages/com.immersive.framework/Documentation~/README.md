# Immersive Framework Documentation

Canonical planning entry point:

```text
Planning/Immersive-Framework-Roadmap-Revisado.md
```

Accepted architectural decisions:

```text
ADRs/ADR-INDEX.md
```


Usage guides:

```text
Guides/F15-Unity-Object-Reset-Adapters-Usage.md
Guides/F16-GameObject-Active-Reset-Usage.md
Guides/F17-Gate-Foundation-Usage.md
Guides/F18-Transition-Orchestration-Usage.md
Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md
Guides/F19-Transition-Effects-Usage.md
Guides/F20-Pause-State-Gate-Usage.md
Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md
```

Closure rule: when a framework phase is closed, add or update its `Usage` guide under `Documentation~/Guides/`.


F0-F22 are closed/applied, with F22 receiving pre-F23 framework-only debt closure. F17 is Gate Foundation. F18 is Transition Orchestration Foundation. F19 is Transition Effects. F20 is Pause State/Gate. F21 is closed for Save / Snapshot / Preferences / Progression Save Foundation with `Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. F22 is closed for Loading Operation / Progress / Readiness Boundary with `Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. F22A is applied for Loading Architecture ADR Plan; F22B is applied for Loading Operation / Step / Weighted Progress Primitives; F22C is applied for Loading Progress Aggregation Smoke; F22D is applied for SceneLifecycle / Transition Loading Observation Adapter; F22E is applied for Loading Screen Adapter Boundary; F22F is applied for Closure + Usage Guide. Pause Content/Overlay/Input moves to F23. Unity Build Surface / Lifecycle Wiring is F24. Gameplay Adapter Foundation moves to F25.
F17A realigned the plan/ADRs; F17B introduced passive Gate primitives; F17C integrates those primitives with existing request-admission guards; F17D added a synthetic QA smoke for Gate admission diagnostics; F17E closes the phase and hands off to F18. F18A accepts the Transition Orchestration implementation plan. F18B introduces passive Transition primitives. F18C adds a synthetic Transition diagnostics smoke for plan/result/snapshot shapes without runtime visual effects. F18D adds a passive Transition-to-Gate blocker relationship and synthetic smoke without registering runtime Gate state. F18E adds passive Route/Activity orchestration observation and smoke without executing requests. F18F closes the phase with `Guides/F18-Transition-Orchestration-Usage.md` and hands off to F19 Transition Effects. F19A accepts the Transition Effects boundary/implementation plan and records that no scene/object/SO setup is required yet. F19B adds passive Transition Effect primitives under `Runtime/TransitionEffects`; F19C adds `Run Transition Effect Diagnostics Smoke`, still without scene/object/SO setup. F19D adds `ITransitionEffectAdapter`, `UnityFadeCurtainEffectAdapter` and `Run Unity Fade Curtain Effect Adapter Smoke`; the smoke uses a transient QA GameObject, while optional manual scene setup is documented in `Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md`. F19E adds required/optional effect policy guardrails and `Run Transition Effect Policy Guardrails Smoke`; no ScriptableObject or saved scene setup is required. F19F closes the phase with `Guides/F19-Transition-Effects-Usage.md` and compacts QA Canvas by keeping baseline buttons visible and moving phase diagnostics behind toggles. F20A accepted the Pause State/Gate plan; F20B adds passive Pause primitives under `Runtime/Pause`; F20C adds `Run Pause Diagnostics Smoke`; F20D adds passive Pause-to-Gate blocker policy; F20E adds the minimal runtime Pause request path through `FrameworkRuntimeHost` and `PauseRuntime`, still without scene/object/SO setup, input, overlay or `Time.timeScale`. F20F closes the phase with `Guides/F20-Pause-State-Gate-Usage.md`.

Current reset boundary:

```text
Cycle Reset is Route/Activity reset only.
Object Reset foundation is closed as logical orchestration only.
Player Reset, Actor Reset and contextual gameplay reset are future phases. Minimal physical Unity reset adapters are Transform local baseline reset and GameObject activeSelf reset.
Contextual reset for Player/Actor/NPC/Timer/Door/Pickup is deferred until after Gate, Transition and Pause, and after a mature gameplay object model exists.
```

Current planning axis:

```text
F17 - Gate Foundation / CLOSED
F18 - Transition Orchestration Foundation / CLOSED
F19 - Transition Effects / CLOSED
F20 - Pause State and Pause Gate / CLOSED / F20F QA PASS + USAGE
F21 - Save / Snapshot / Preferences / Progression Save Foundation / CLOSED / F21H QA PASS + USAGE
F22 - Loading Operation / Progress / Readiness Boundary / CLOSED / F22F QA PASS + USAGE
F23 - Pause Content / Overlay / Input Boundary / F23C APPLIED / NEXT F23D
F24 - Unity Build Surface / Lifecycle Wiring
F25 - Gameplay Adapter Foundation
```

Cycle Reset authoring note:

```text
RouteCycleResetTrigger and ActivityCycleResetTrigger are the primary components.
Unity Event Bridges are optional and are only needed for Inspector/UnityEvent callbacks.
```

Closed Object Entry boundary:

```text
Object Entry is a passive lifecycle-owned logical catalog/snapshot.
It is not GameObject binding, Object Reset, a mutable registry or a service locator.
Route/Activity owners, scoped collection and snapshot lifecycle were closed in F13.
```

Current Object Reset boundary:

```text
F14 closed ObjectResetTarget as ObjectEntryId + owner + scope from the current Object Entry snapshot.
Object Reset has request/policy/result, target resolver, participant contract/source, deterministic plan/runtime executor, Runtime Host integration, public trigger and optional UnityEvent bridge.
Transform/Rigidbody/Animator, pooling, Player/Actor and gameplay reset remain outside F14. F15 closed the minimal technical Unity reset adapter path with an explicit participant source, Transform local baseline reset, required guardrails, authoring UX and closure smoke. F16 added GameObject activeSelf reset as a second primitive adapter. Gameplay reset remains outside F15/F16.
```


Closed Object Reset usage note:

```text
Author a current Object Entry Declaration.
Add Object Reset Trigger and reference that declaration.
UGUI/Button may call ObjectResetTrigger.RequestObjectReset() directly.
Object Reset Trigger Unity Event Bridge is optional and only adapts trigger events to Inspector UnityEvents.
With F15 adapters, a valid authored trigger can execute Transform reset when a participant source and Transform Reset Participant are configured. SucceededNoParticipants remains valid only when policy allows no participants; required adapter/baseline absence must be explicit.
```


Closed F15 adapter boundary note:

```text
Unity Reset Adapters are technical IObjectResetParticipant implementations. F15 includes Transform Reset Participant. F16 includes GameObject Active Reset Participant.
They must target Object Entry identity, not GameObject.name/path.
Required adapter/source absence must be explicit and cannot be hidden by SucceededNoParticipants.
Player, Actor, Pooling, Save/Checkpoint and gameplay reset stay outside F15/F16 and remain deferred past F17-F24 into F25+.
```


F19 authoring note:

```text
No Unity scene object, component setup or ScriptableObject is required in F19A-F19C.
F19D introduces the first concrete Unity adapter. The canonical QA smoke does not require saved scene setup because it creates a transient QA surface. Manual visual testing can be done in a QA scene with a GameObject containing CanvasGroup + UnityFadeCurtainEffectAdapter.
F19E keeps authoring policy asset-free: no ScriptableObject is required. The policy evaluates a `TransitionEffectPlan` against an explicit adapter list supplied by the caller or smoke.
F19F closes the phase and documents usage in `Guides/F19-Transition-Effects-Usage.md`. QA Canvas is compacted: Standard Smoke, Activity Baseline Smoke, Validate Loaded Authoring and Reset QA Scenario stay visible; phase diagnostics are opened only when needed.
```

F20 pause note:

```text
F20B adds passive Pause primitives under `Runtime/Pause`. F20C adds synthetic Pause diagnostics smoke. F20D adds passive Pause Gate blocker policy. F20E adds minimal in-memory runtime request execution. F20F closes the phase with `Guides/F20-Pause-State-Gate-Usage.md`. No scene, GameObject, Canvas, prefab or ScriptableObject is required for F20.
F20 is Pause logical core: state, request/result, policy, snapshot/facts and Gate blocker relationship.
Save/Snapshot/Preferences/Progression Save belongs to F21. Loading Operation/Progress/Readiness belongs to F22. Pause content/overlay/input moves to F23.
F21H closes the phase with `Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. F22 is closed with `Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. F22A through F22F are applied. Next cut: F23D - Pause Input Boundary Contracts.
```

F21/F22 boundary note:

```text
Snapshot does not know backend.
Preferences does not use progression slots.
Progression Save uses a replaceable backend port.
The JSON backend is the initial adapter, not the canonical contract.
A future premium backend must swap behind the same interface.
Loading is operation/progress/readiness reporting, not fade, curtain, loading screen prefab, TransitionEffect vocabulary or SceneLifecycle replacement.
Loading visual belongs to a later adapter.
```


F21A result:

```text
Documentation-only roadmap/ADR realignment.
Snapshot, Preferences and Progression Save are separate boundaries.
Loading Operation/Progress/Readiness is F22. F22A accepts the architecture plan and reconciles existing loading-like concepts so SceneLifecycle, Transition and TransitionEffects do not become parallel Loading tracks.
Pause Content/Overlay/Input is F23.
Unity Build Surface / Lifecycle Wiring is F24.
Gameplay Adapter Foundation is F25.
No runtime, asmdef, backend, PlayerPrefs, JSON, UI, scene object, prefab or ScriptableObject was added.
F21H closes the phase with `Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. F22 is closed with `Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. F22A through F22F are applied. Next cut: F23D - Pause Input Boundary Contracts.
```

F24/F25 ordering note:

```text
F24 comes after F23.
F24 comes before gameplay adapters.
F24 is Unity Build Surface / Lifecycle Wiring, not gameplay adapter work.
F24B - Transition ↔ GameFlow Runtime Integration is the first technical cut of F24.
F25 is Gameplay Adapter Foundation.
```


## F21B Snapshot Envelope Primitives

F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`: envelope id, scope, schema id, schema version, payload format, payload and envelope.

The cut does not add backend, PlayerPrefs, JSON, progression slot, manifest, participant interface, capture/restore runtime, UI, scene object, prefab, ScriptableObject or asmdef changes.

## F21C Snapshot Participant Contracts + Diagnostics Smoke

F21C adds backend-agnostic Snapshot participant contracts under `Runtime/Snapshot`: participant id, requiredness, descriptor, capture/restore contexts, capture/restore results and `ISnapshotParticipant`.

F21C adds `Run Snapshot Participant Diagnostics Smoke` under `Show Save / Snapshot diagnostics` in the QA Canvas. The smoke validates descriptor/context matching, synthetic capture, synthetic restore, foreign-envelope rejection, optional skip behavior and that known diagnostic/runtime snapshot types remain outside the canonical Save Snapshot namespace.

F21C does not add participant discovery, Snapshot orchestration runtime, backend, PlayerPrefs, JSON, Progression Save slots/manifests, autosave/load moments, UI, scene object, prefab, ScriptableObject or asmdef changes. `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot` and `ObjectEntryRuntimeContextSnapshot` remain diagnostic/runtime state snapshots, not Save Snapshot contracts.


## F21D Preferences Store Contracts + PlayerPrefs Backend

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


F21E applied note: F21E adds Progression Save port and slot/manifest primitives under `Runtime/ProgressionSave`: `ProgressionSaveSlotId`, `ProgressionSaveRecordId`, `ProgressionSaveBackendId`, `ProgressionSavePayloadFormat`, `ProgressionSavePayload`, `ProgressionSaveSlotRecord`, `ProgressionSaveManifestEntry`, `ProgressionSaveManifest`, status/result primitives and `IProgressionSaveStore`. It adds `ProgressionSave` to `FrameworkIdentityDomain`. It does not add a concrete backend, JSON, file paths, PlayerPrefs, autosave/load moments, runtime save request path, UI, scene object, prefab, ScriptableObject or asmdef changes.

F21F applied note: F21F adds `JsonProgressionSaveStore` behind `IProgressionSaveStore` and `Run Progression Save JSON Backend Smoke`. The smoke validates missing, write/read, manifest, corrupt slot, delete cleanup and boundary separation. JSON/file paths are adapter details; no Snapshot backend, Preferences usage, PlayerPrefs, autosave/load moments, runtime request path, UI, scene object, prefab, ScriptableObject or asmdef changes are introduced.


F21G applied note: F21G adds `ProgressionSaveRuntime`, `ProgressionSaveRequest`, `ProgressionSaveRequestResult`, `ProgressionSaveMoment` and related ids/enums. Runtime requests execute explicit save/load/delete operations against `IProgressionSaveStore`. Autosave moments are passive descriptors only; F21G does not add an autosave scheduler, Route/Activity hook, Snapshot capture orchestration, Preferences/PlayerPrefs usage, UI, scene object, prefab, ScriptableObject or asmdef changes. It adds `Run Progression Save Runtime Request Smoke`. F21H closes the phase with `Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md`. F22 is closed with `Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. F22A through F22F are applied. Next cut: F23D - Pause Input Boundary Contracts.


F22A applied note: F22A is documentation-only. It accepts the Loading architecture plan. Next cut: F23D - Pause Input Boundary Contracts.


F22B applied note: F22B adds passive Loading primitives under `Runtime/Loading`: `LoadingOperationId`, `LoadingStepId`, operation/step statuses, normalized `LoadingProgress`, `LoadingStepWeight`, `LoadingWeightedProgress`, `LoadingStep` and `LoadingOperation`. It also adds `FrameworkIdentityDomain.Loading`.

F22C applied note: F22C adds `LoadingProgressAggregationStatus`, `LoadingProgressAggregationResult`, `LoadingProgressAggregator` and `Run Loading Progress Aggregation Smoke` under QA Canvas `Show Loading diagnostics`. It adds no SceneLifecycle/Transition adapter, readiness wait contract, LoadingResult/LoadingFailure record, UI, fade, curtain, loading screen prefab, scene object, prefab, ScriptableObject, backend, PlayerPrefs, JSON or asmdef changes.

F22D applied note: F22D adds `LoadingObservationAdapter` and `Run Loading Observation Adapter Smoke`. It observes existing SceneLifecycle and Transition diagnostics as canonical Loading progress records without executing scene lifecycle, replacing Transition, running effects, mutating readiness or creating UI. Next cut: F23D - Pause Input Boundary Contracts.

F22E applied note: F22E adds loading screen adapter contracts under `Runtime/Loading`: `ILoadingScreenAdapter`, `LoadingScreenPresentation`, `LoadingScreenAdapterAction`, `LoadingScreenAdapterStatus` and `LoadingScreenAdapterResult`. It also adds `Run Loading Screen Adapter Boundary Smoke`. The boundary consumes canonical `LoadingOperation` data and does not create UI, scene objects, prefabs, ScriptableObjects, fade, curtain, TransitionEffect execution, SceneLifecycle execution, Transition execution, readiness mutation, backend, PlayerPrefs, JSON or asmdef changes. Next cut: F23D - Pause Input Boundary Contracts.


## F22 Loading Boundary Closure

F22 is closed with `Guides/F22-Loading-Operation-Progress-Readiness-Usage.md`. The phase defines Loading as operation/progress/readiness reporting and adapter boundaries only. It does not create a concrete loading screen UI, prefab, scene object, ScriptableObject, fade/curtain execution, SceneLifecycle replacement, Transition replacement, readiness mutation, backend, PlayerPrefs, JSON or asmdef change.

Next cut: F23D - Pause Input Boundary Contracts.


## F22G pre-F23 debt closure

F22G adds the canonical Loading readiness observation primitives and `Run Loading Readiness Observation Smoke`. This closes the readiness-observation part of the F22 debt without gameplay adapters, UI, SceneLifecycle execution, Transition execution or readiness mutation.


## F22H pre-F23 debt closure

F22H adds canonical Loading result/issue primitives and `Run Loading Result and Issue Smoke`. This closes the result/reporting part of the F22 debt without retry/fallback behavior, lifecycle execution, readiness mutation, UI or gameplay adapters.


## F23A Pause Content / Overlay / Input ADR Plan

F23A accepts `F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md` and opens F23 as a consumer boundary over the already-closed F20 Pause core.

F23A is documentation-only. It does not add runtime code, asmdef changes, scene objects, Canvas/prefab setup, ScriptableObjects, input asset binding, `Time.timeScale` policy, Save/Loading execution or gameplay adapters.

Next cut: F23D - Pause Input Boundary Contracts.


## F23B Pause Content Anchor Consumer Contracts

F23B adds passive Pause Content Anchor consumer contracts under `Runtime/Pause`: `PauseContentAnchorRequestId`, `PauseContentAnchorPurpose`, `PauseContentAnchorRequest`, `PauseContentAnchorConsumerStatus`, `PauseContentAnchorConsumerResult` and `IPauseContentAnchorConsumer`.

The contracts bridge Pause intent to the canonical Content Anchor binding vocabulary by preparing `ContentAnchorBindingRequest` data. They do not create anchors, materialize UI, bind input, mutate Pause state, execute Transition Effects, change `Time.timeScale`, own Route/Activity lifecycle or add gameplay adapters.

Validation for F23B is compile/import only. The dedicated Pause Content / Overlay / Input diagnostics smoke remains planned for F23E after F23C/F23D add overlay and input contracts.

## F23C Pause Overlay Adapter Boundary

F23C adds visual-facing Pause overlay adapter contracts under `Runtime/Pause`: `IPauseOverlayAdapter`, `PauseOverlayPresentation`, `PauseOverlayAdapterAction`, `PauseOverlayAdapterStatus` and `PauseOverlayAdapterResult`.

The overlay boundary presents canonical `PauseSnapshot` data and optional prepared Pause Content Anchor consumer data. It does not create UI, Canvas, prefabs, ScriptableObjects, input binding, `Time.timeScale` policy, TransitionEffect execution, anchor creation, scene object discovery, Route/Activity lifecycle ownership or gameplay adapters.

Validation for F23C is compile/import only. The dedicated Pause Content / Overlay / Input diagnostics smoke remains planned for F23E after F23D adds input contracts.
