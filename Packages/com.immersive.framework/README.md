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
```


Status:

```text
F0-F20 closed/applied. F21 is open. F21A is applied/documental; F21B Snapshot Envelope Primitives are applied; F21C Snapshot Participant Contracts + Diagnostics Smoke is applied; F21D Preferences Store Contracts + PlayerPrefs Backend is applied; F21E is next/planned.
F17 is Gate Foundation and is closed through F17E. F18 is Transition Orchestration Foundation and is closed through F18F. F19 is Transition Effects and is closed through F19F. F20 is Pause State/Gate and is closed through F20F with `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`. F20A accepted the Pause State/Gate implementation plan. F20B introduced passive Pause state primitives under `Runtime/Pause`. F20C added `Run Pause Diagnostics Smoke`. F20D added passive Pause-to-Gate blocker policy. F20E added the minimal runtime Pause request path through `FrameworkRuntimeHost` and `PauseRuntime`. F20F closes the phase and hands off to F21 Save / Snapshot / Preferences / Progression Save Foundation. F21A realigns the roadmap and accepts Save/Loading boundaries; F21B adds passive Snapshot envelope primitives under `Runtime/Snapshot`; F21C adds Snapshot participant contracts and a synthetic diagnostics smoke; F21D adds Preferences store contracts and the PlayerPrefs adapter; F21E is the next implementation cut.
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
F19 - Transition Effects / Loading and Fade Adapters / CLOSED
F20 - Pause State and Pause Gate / CLOSED / F20F QA PASS + USAGE
F21 - Save / Snapshot / Preferences / Progression Save Foundation / IN PROGRESS / F21D PREFERENCES APPLIED / F21E NEXT
F22 - Loading Operation / Progress / Readiness Boundary
F23 - Pause Content / Overlay / Input Boundary
F24 - Gameplay Adapter Foundation
```


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
Save/Snapshot/Preferences/Progression Save are F21 boundaries. Loading operation/progress/readiness is F22. Pause visual content, overlay and input binding move to F23.
F20C adds `Run Pause Diagnostics Smoke` under `Show Pause diagnostics`, validating passive Pause requests/results/snapshots. F20D adds `Run Pause Gate Blocker Smoke`. F20E adds `Run Pause Runtime Request Smoke`, which exercises the real in-memory Pause request path and leaves the runtime in `Running`. F20F closes the phase with `Documentation~/Guides/F20-Pause-State-Gate-Usage.md`.
```


### F20D — Pause-to-Gate Blocker Policy

F20D adds passive Pause-to-Gate blocker policy diagnostics. It does not create a Pause runtime owner, input binding, overlay, Time.timeScale adapter or Gate registry.

### F20E — Minimal Runtime Pause Request Path

F20E adds `PauseRuntime` and `FrameworkRuntimeHost.RequestPause(...)`. It mutates only in-memory Pause state and derived Pause Gate snapshots. It does not read input, show overlay/menu UI, change `Time.timeScale`, own Route/Activity lifecycle or register blockers in a global Gate runtime.


### F20F — Pause State/Gate Closure

F20 is closed as logical Pause core. It provides state, request/result, runtime snapshot and passive Gate blocker relationship. It intentionally does not provide Pause menu, overlay content, input binding, `Time.timeScale` adapter, scene object setup or ScriptableObject authoring.

Use `Documentation~/Guides/F20-Pause-State-Gate-Usage.md` for the current usage boundary and QA smokes. F21A is the applied documentation cut for Save / Snapshot / Preferences / Progression realignment. F21B adds Snapshot Envelope primitives and keeps the boundary backend-free. F21C adds Snapshot participant contracts and `Run Snapshot Participant Diagnostics Smoke`. F21D adds Preferences store contracts, `PlayerPrefsPreferencesStore` and `Run Preferences Store Diagnostics Smoke`. F21E is next for Progression Save Port + Slot/Manifest Primitives.

### F21/F22 roadmap realignment

F21A opens Save before Pause visual/gameplay work. Snapshot is an envelope/participant boundary and does not know persistence backend. Preferences are independent user/application settings and do not use progression slots. Progression Save owns slot/manifest/request contracts and uses a replaceable backend port. A future JSON backend is only the initial adapter, not the canonical contract; a future premium backend must swap behind the same interface.

F22 is now reserved for Loading as operation/progress/readiness reporting. Loading is not fade, curtain, loading screen prefab or a SceneLifecycle replacement. Loading visuals remain a later adapter boundary.


### F21A — Save/Loading ADR Plan Result

F21A is documentation-only. It does not add runtime code, asmdefs, PlayerPrefs, JSON, backend, UI, scene object, prefab or ScriptableObject setup. It records that Snapshot, Preferences and Progression Save are separate boundaries, and that Loading Operation/Progress/Readiness is its own F22 boundary before Pause visual work.

F21B adds passive Snapshot primitives under `Runtime/Snapshot`: `SnapshotEnvelopeId`, `SnapshotScope`, `SnapshotSchemaId`, `SnapshotSchemaVersion`, `SnapshotPayloadFormat`, `SnapshotPayload` and `SnapshotEnvelope`. These types do not save files, use PlayerPrefs, choose JSON, bind progression slots, restore gameplay, create runtime objects or define participant contracts.

F21C adds backend-agnostic Snapshot participant contracts and a synthetic diagnostics smoke. It does not add discovery, orchestration runtime, backend, PlayerPrefs, JSON, progression slots or UI.

Next cut: `F21E — Progression Save Port + Slot/Manifest Primitives`.


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

Next cut: `F21E — Progression Save Port + Slot/Manifest Primitives`.
